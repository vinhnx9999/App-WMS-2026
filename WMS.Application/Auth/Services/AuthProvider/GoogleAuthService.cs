using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using WMS.Application.Auth.DTOs.Google;
using WMS.Application.Auth.Interfaces;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.Security;
using WMS.Domain.Interfaces;

namespace WMS.Application.Auth.Services.AuthProvider;

public class GoogleAuthService(
    IUnitOfWork uow,
    IConfiguration config,
    IHttpClientFactory httpFactory,
    ILogger<GoogleAuthService> log) : 
    BaseAuthService(config), IGoogleAuthService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly IConfiguration _config = config;
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILogger<GoogleAuthService> _log = log;
    
    public async Task<GoogleLoginResponse> LoginWithIdTokenAsync(
        string idToken, CancellationToken ct)
    {
        // ── Bước 1: Verify Google ID Token ──
        var googleUser = await VerifyGoogleIdTokenAsync(idToken, ct);

        _log.LogInformation(
            "Google ID token verified: email={Email} googleId={Id}",
            googleUser.Email, googleUser.GoogleId);

        // ── Bước 2: Find or create WMS user ──
        return await FindOrCreateUserAsync(googleUser, ct);
    }

    public async Task<GoogleLoginResponse> LoginWithAuthCodeAsync(
        string code, string redirectUri, CancellationToken ct)
    {
        // ── Bước 1: Exchange code for tokens ──
        var tokens = await ExchangeCodeForTokensAsync(code, redirectUri, ct);

        // ── Bước 2: Verify ID token ──
        var googleUser = await VerifyGoogleIdTokenAsync(tokens.IdToken, ct);

        _log.LogInformation(
            "Google auth code verified: email={Email}",
            googleUser.Email);

        // ── Bước 3: Find or create WMS user ──
        return await FindOrCreateUserAsync(googleUser, ct);
    }

    public string GetGoogleAuthUrl(string redirectUri, string? state = null)
    {
        var clientId = _config["Google:ClientId"]
            ?? throw new InvalidOperationException("Google:ClientId not configured");

        var queryParams = System.Web.HttpUtility.ParseQueryString("");
        queryParams["client_id"] = clientId;
        queryParams["redirect_uri"] = redirectUri;
        queryParams["response_type"] = "code";
        queryParams["scope"] = "openid email profile";
        queryParams["access_type"] = "offline";
        queryParams["prompt"] = "consent";

        if (!string.IsNullOrEmpty(state))
            queryParams["state"] = state;

        return $"{GoogleAuthEndpoint}?{queryParams}";
    }

    private async Task<GoogleUserInfo> VerifyGoogleIdTokenAsync(
        string idToken, CancellationToken ct)
    {
        var clientId = _config["Google:ClientId"]
            ?? throw new InvalidOperationException("Google:ClientId not configured");

        try
        {
            // Load Google's OIDC discovery document để lấy signing keys
            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                "https://accounts.google.com/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever(_httpFactory.CreateClient()));

            var oidcConfig = await configManager.GetConfigurationAsync(ct);

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = GoogleIssuer,
                ValidateAudience = true,
                ValidAudience = clientId,
                ValidateLifetime = true,
                IssuerSigningKeys = oidcConfig.SigningKeys,
                ClockSkew = TimeSpan.FromMinutes(5),
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(
                idToken, validationParams, out var _);

            var sub = principal.FindFirst("sub")?.Value ?? "";
            var email = principal.FindFirst("email")?.Value ?? "";
            var name = principal.FindFirst("name")?.Value
                    ?? principal.FindFirst("email")?.Value ?? "";
            var picture = principal.FindFirst("picture")?.Value;
            var emailVerified = principal.FindFirst("email_verified")?.Value == "true";

            if (string.IsNullOrEmpty(email))
                throw new AppException(401, "GOOGLE_AUTH", "Token không chứa email");

            if (!emailVerified)
                throw new AppException(401, "GOOGLE_AUTH", "Email chưa được xác minh");

            return new GoogleUserInfo
            {
                GoogleId = sub,
                Email = email.ToLowerInvariant(),
                Name = name,
                Picture = picture,
                EmailVerified = emailVerified,
            };
        }
        catch (SecurityTokenException ex)
        {
            _log.LogWarning(ex, "Google ID token validation failed");
            throw new AppException(401, "GOOGLE_AUTH", "Token Google không hợp lệ");
        }
    }

    private async Task<(string IdToken, string? AccessToken)> ExchangeCodeForTokensAsync(
        string code, string redirectUri, CancellationToken ct)
    {
        var clientId = _config["Google:ClientId"]!;
        var clientSecret = _config["Google:ClientSecret"]!;

        var http = _httpFactory.CreateClient();
        var response = await http.PostAsync(GoogleTokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code",
            }), ct);

        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(ct);
            _log.LogError("Google token exchange failed: {Status} {Body}",
                response.StatusCode, errBody);
            throw new AppException(401, "GOOGLE_AUTH", "Không thể đổi code lấy token");
        }

        var tokenResponse = await response.Content
            .ReadFromJsonAsync<GoogleTokenResponse>(ct)
            ?? throw new AppException(401, "GOOGLE_AUTH", "Empty token response");

        return (tokenResponse.IdToken, tokenResponse.AccessToken);
    }

    private async Task<GoogleLoginResponse> FindOrCreateUserAsync(
        GoogleUserInfo googleUser, CancellationToken ct)
    {
        var userRepo = _uow.Repository<User>();
        var isNew = false;

        // ── Tìm theo GoogleId ──
        var users = await userRepo.FindAsync(
            u => u.GoogleId == googleUser.GoogleId, ct);
        var user = users.FirstOrDefault();

        // ── Nếu chưa có, tìm theo Email ──
        if (user is null)
        {
            users = await userRepo.FindAsync(
                u => u.Email == googleUser.Email, ct);
            user = users.FirstOrDefault();

            // Link Google account vào user hiện có
            if (user is not null)
            {
                user.GoogleId = googleUser.GoogleId;
                user.AvatarUrl = googleUser.Picture;
                user.AuthProvider = "google";
                _log.LogInformation(
                    "Linked Google to existing user: {Email}",
                    googleUser.Email);
            }
        }

        // ── Nếu vẫn chưa có, tạo mới ──
        if (user is null)
        {
            // Gán role mặc định "viewer" — admin sẽ nâng cấp sau
            var viewerRole = (await _uow.Repository<Role>()
                .FindAsync(r => r.Name == "viewer", ct))
                .FirstOrDefault()
                ?? throw new AppException(500, "SEED", "Role 'viewer' chưa tồn tại");

            user = new User
            {
                Email = googleUser.Email,
                FullName = googleUser.Name,
                GoogleId = googleUser.GoogleId,
                AvatarUrl = googleUser.Picture,
                AuthProvider = "google",
                RoleId = viewerRole.Id,
                PasswordHash = "",  // No password for Google users
                IsActive = true,
            };

            await userRepo.AddAsync(user, ct);
            isNew = true;

            _log.LogInformation(
                "Created new user from Google: {Email} role={Role}",
                user.Email, viewerRole.Name);
        }

        // ── Kiểm tra active ──
        if (!user.IsActive)
            throw new AppException(403, "DISABLED", "Tài khoản đã bị vô hiệu hóa");

        // ── Update last login ──
        user.LastLoginAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);

        // ── Load role ──
        var role = await _uow.Repository<Role>()
            .GetByIdAsync(user.RoleId, ct);

        // ── Generate WMS JWT ──        
        var token = GenerateAccessToken(user, role!);
        var expiry = int.Parse(
            _config["Jwt:AccessTokenExpiryMinutes"] ?? "60");

        return new GoogleLoginResponse
        {
            AccessToken = token,
            ExpiresIn = expiry * 60,
            User = new(user.Id, user.Email, user.FullName, role?.Name ?? ""),
            IsNewUser = isNew,
        };
    }    
}
