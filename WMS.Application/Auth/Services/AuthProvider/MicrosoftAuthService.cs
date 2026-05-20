using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Web;
using WMS.Application.Auth.DTOs.Microsoft;
using WMS.Application.Auth.Interfaces;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.Security;
using WMS.Domain.Interfaces;

namespace WMS.Application.Auth.Services.AuthProvider;

public class MicrosoftAuthService(
    IUnitOfWork uow,
    IConfiguration config,
    IHttpClientFactory httpFactory,
    ILogger<MicrosoftAuthService> log) : BaseAuthService(config), IMicrosoftAuthService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly IConfiguration _config = config;
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILogger<MicrosoftAuthService> _log = log;

    public string GetAuthUrl(string redirectUri, string? state = null)
    {
        var clientId = _config["Microsoft:ClientId"]
            ?? throw new InvalidOperationException("Microsoft:ClientId not configured");

        var q = HttpUtility.ParseQueryString("");
        q["client_id"] = clientId;
        q["response_type"] = "code";
        q["redirect_uri"] = redirectUri;
        q["scope"] = "openid email profile User.Read";
        q["response_mode"] = "query";
        q["state"] = state ?? "/";
        q["prompt"] = "select_account";  // Cho user chọn account

        var authUrl = $"{string.Format(MsAuthTemplate, Tenant)}?{q}";

        _log.LogInformation("MS auth URL generated: tenant={Tenant}", Tenant);

        return authUrl;
    }

    public async Task<MicrosoftLoginResponse> LoginWithCodeAsync(
        string code, string redirectUri, CancellationToken ct)
    {
        // ── Bước 1: Exchange code for tokens ──
        var tokens = await ExchangeCodeAsync(code, redirectUri, ct);

        // ── Bước 2: Verify ID token (local OIDC) ──
        var msUser = await VerifyIdTokenAsync(tokens.IdToken, ct);

        _log.LogInformation(
            "MS user authenticated: oid={Oid} email={Email} tenant={Tenant}",
            msUser.Oid, msUser.Email, msUser.TenantId);

        // ── Bước 3: Find or create WMS user ──
        return await FindOrCreateUserAsync(msUser, ct);
    }

    private async Task<MsTokenResponse> ExchangeCodeAsync(
        string code, string redirectUri, CancellationToken ct)
    {
        var clientId = _config["Microsoft:ClientId"]!;
        var clientSecret = _config["Microsoft:ClientSecret"]!;
        var tokenEndpoint = string.Format(MsTokenTemplate, Tenant);

        var http = _httpFactory.CreateClient();

        var body = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
        };

        var response = await http.PostAsync(tokenEndpoint,
            new FormUrlEncodedContent(body), ct);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            _log.LogError("MS token exchange failed: {Status} {Body}",
                response.StatusCode, err);
            throw new AppException(401, "MS_AUTH",
                "Không thể đổi code lấy token từ Microsoft");
        }

        return (await response.Content
            .ReadFromJsonAsync<MsTokenResponse>(ct))!;
    }

    private async Task<MicrosoftUserInfo> VerifyIdTokenAsync(
        string idToken, CancellationToken ct)
    {
        var clientId = _config["Microsoft:ClientId"]!;
        var oidcConfigUrl = string.Format(MsOidcTemplate, Tenant);

        try
        {
            // Load Microsoft OIDC discovery document
            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                oidcConfigUrl,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever(_httpFactory.CreateClient()));

            var oidcConfig = await configManager.GetConfigurationAsync(ct);

            // Valid issuers — Microsoft có nhiều issuer
            var validIssuers = new[]
            {
                $"https://login.microsoftonline.com/{Tenant}/v2.0",
                $"https://login.microsoftonline.com/9188040d-6c67-4c5b-b112-36a304b66dad/v2.0", // MS consumer tenant
            };

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = validIssuers,
                ValidateAudience = true,
                ValidAudience = clientId,  // audience = client_id
                ValidateLifetime = true,
                IssuerSigningKeys = oidcConfig.SigningKeys,
                ClockSkew = TimeSpan.FromMinutes(5),
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(
                idToken, validationParams, out var _);

            // Extract claims
            var oid = principal.FindFirst("oid")?.Value ?? "";
            var email = principal.FindFirst("email")?.Value
                      ?? principal.FindFirst("preferred_username")?.Value
                      ?? "";
            var name = principal.FindFirst("name")?.Value ?? "";
            var preferredUsername = principal.FindFirst("preferred_username")?.Value;
            var tid = principal.FindFirst("tid")?.Value;

            // Verify email present
            if (string.IsNullOrEmpty(email))
                throw new AppException(401, "MS_AUTH",
                    "Microsoft token không chứa email");

            return new MicrosoftUserInfo
            {
                Oid = oid,
                Email = email.ToLowerInvariant(),
                Name = name,
                PreferredUsername = preferredUsername,
                TenantId = tid,
            };
        }
        catch (SecurityTokenException ex)
        {
            _log.LogWarning(ex, "MS ID token validation failed");
            throw new AppException(401, "MS_AUTH",
                "Token Microsoft không hợp lệ");
        }
    }

    private async Task<MicrosoftLoginResponse> FindOrCreateUserAsync(
        MicrosoftUserInfo msUser, CancellationToken ct)
    {
        var userRepo = _uow.Repository<User>();
        var isNew = false;

        // ── 1. Tìm theo MicrosoftId (oid) ──
        var users = await userRepo.FindAsync(
            u => u.MicrosoftId == msUser.Oid, ct);
        var user = users.FirstOrDefault();

        // ── 2. Tìm theo Email ──
        if (user is null)
        {
            users = await userRepo.FindAsync(
                u => u.Email == msUser.BestEmail.ToLowerInvariant(), ct);
            user = users.FirstOrDefault();

            // Link Microsoft account
            if (user is not null)
            {
                user.MicrosoftId = msUser.Oid;
                user.MicrosoftTenantId = msUser.TenantId;
                _log.LogInformation(
                    "Linked Microsoft to existing user: {Email}",
                    msUser.Email);
            }
        }

        // ── 3. Tạo mới ──
        if (user is null)
        {
            var viewerRole = (await _uow.Repository<Role>()
                .FindAsync(r => r.Name == "viewer", ct))
                .FirstOrDefault()
                ?? throw new AppException(500, "SEED", "Role 'viewer' chưa tồn tại");

            user = new User
            {
                Email = msUser.BestEmail.ToLowerInvariant(),
                FullName = msUser.Name,
                MicrosoftId = msUser.Oid,
                MicrosoftTenantId = msUser.TenantId,
                AuthProvider = "microsoft",
                RoleId = viewerRole.Id,
                PasswordHash = "",
                IsActive = true,
            };

            await userRepo.AddAsync(user, ct);
            isNew = true;

            _log.LogInformation(
                "Created user from Microsoft: {Email} oid={Oid} tenant={Tenant}",
                user.Email, msUser.Oid, msUser.TenantId);
        }

        // ── 4. Kiểm tra active ──
        if (!user.IsActive)
            throw new AppException(403, "DISABLED", "Tài khoản đã bị vô hiệu hóa");

        // ── 5. Update ──
        user.LastLoginAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);

        // ── 6. Generate JWT ──
        var role = await _uow.Repository<Role>()
            .GetByIdAsync(user.RoleId, ct);

        var jti = Guid.NewGuid().ToString();
        var accessToken = GenerateAccessToken(user, role!, jti);

        var accessExpiry = int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60");
        var refreshExpiry = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");

        return new MicrosoftLoginResponse
        {
            AccessToken = accessToken,
            ExpiresIn = accessExpiry * 60,
            RefreshExpiresIn = refreshExpiry * 86400,
            User = new(user.Id, user.Email, user.FullName, role?.Name ?? ""),
            IsNewUser = isNew,
        };
    }
}
