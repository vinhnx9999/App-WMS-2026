using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Web;
using WMS.Application.Auth.DTOs.Twitter;
using WMS.Application.Auth.DTOs.Twitter.XResponse;
using WMS.Application.Auth.Interfaces;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.Security;
using WMS.Domain.Interfaces;
using Role = WMS.Domain.Entities.Security.Role;

namespace WMS.Application.Auth.Services.AuthProvider;

public class XAuthService(
    IUnitOfWork uow,
    IConfiguration config,
    IHttpClientFactory httpFactory,
    IConnectionMultiplexer? redis,
    ILogger<XAuthService> log) : 
    BaseAuthService(config), IXAuthService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly IConfiguration _config = config;
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILogger<XAuthService> _log = log;
    private readonly IConnectionMultiplexer? _redis = redis;

    public async Task<XAuthRedirectResponse> GetAuthUrlAsync(
        string redirectUri, CancellationToken ct)
    {
        var clientId = _config["X:ClientId"]
            ?? throw new InvalidOperationException("X:ClientId not configured");

        // ── Generate PKCE ──
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        var state = GenerateState();

        // ── Store verifier in Redis (TTL = 5 min) ──
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(
                $"{PkcePrefix}{state}",
                $"{codeVerifier}|{redirectUri}",
                TimeSpan.FromMinutes(5));
        }

        // ── Build authorization URL ──
        var q = HttpUtility.ParseQueryString("");
        q["response_type"] = "code";
        q["client_id"] = clientId;
        q["redirect_uri"] = redirectUri;
        q["scope"] = "users.read offline.access";
        q["state"] = state;
        q["code_challenge"] = codeChallenge;
        q["code_challenge_method"] = "S256";

        var authUrl = $"{XAuthEndpoint}?{q}";

        _log.LogInformation("X auth URL generated: state={State}", state);

        return new XAuthRedirectResponse
        {
            AuthUrl = authUrl,
            State = state,
        };
    }

    public async Task<XLoginResponse> LoginWithCodeAsync(
        string code, string state, string redirectUri,
        CancellationToken ct)
    {
        // ── Retrieve code_verifier from Redis ──
        var codeVerifier = await RetrieveCodeVerifierAsync(state, ct);

        // ── Exchange code for access_token ──
        var tokenResponse = await ExchangeCodeAsync(
            code, codeVerifier, redirectUri, ct);

        if (string.IsNullOrEmpty(tokenResponse.AccessToken))
            throw new AppException(401, "X_AUTH", "Không lấy được access token từ X");

        // ── Get user info from X API v2 ──
        var xUser = await GetXUserInfoAsync(tokenResponse.AccessToken, ct);

        _log.LogInformation(
            "X user authenticated: id={Id} username={Username} name={Name}",
            xUser.XId, xUser.Username, xUser.Name);

        // ── Find or create WMS user ──
        return await FindOrCreateUserAsync(xUser, ct);
    }


    //TODO: later, implement refresh token flow
    private async Task<string> RetrieveCodeVerifierAsync(
        string state, CancellationToken ct)
    {
        if (_redis == null)
            throw new AppException(500, "CONFIG", "Redis not available for PKCE");

        var db = _redis.GetDatabase();
        var stored = await db.StringGetAsync($"{PkcePrefix}{state}");

        if (stored.IsNullOrEmpty)
            throw new AppException(400, "INVALID_STATE",
                "State không hợp lệ hoặc đã hết hạn. Thử đăng nhập lại.");

        // Xóa sau khi dùng (one-time use)
        await db.KeyDeleteAsync($"{PkcePrefix}{state}");

        var parts = ((string)stored!).Split('|', 2);
        return parts[0]; // code_verifier
    }

    private async Task<XTokenResponse> ExchangeCodeAsync(
        string code, string codeVerifier, string redirectUri,
        CancellationToken ct)
    {
        var clientId = _config["X:ClientId"]!;

        var http = _httpFactory.CreateClient();

        var body = new Dictionary<string, string>
        {
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier,
        };

        var request = new HttpRequestMessage(HttpMethod.Post, XTokenEndpoint)
        {
            Content = new FormUrlEncodedContent(body),
        };

        // X requires Basic auth with client_id:client_secret
        var clientSecret = _config["X:ClientSecret"];
        if (!string.IsNullOrEmpty(clientSecret))
        {
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);
        }

        var response = await http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            _log.LogError("X token exchange failed: {Status} {Body}",
                response.StatusCode, err);
            throw new AppException(401, "X_AUTH",
                "Không thể đổi code lấy token từ X");
        }

        return (await response.Content
            .ReadFromJsonAsync<XTokenResponse>(ct))!;
    }

    private async Task<XUserInfo> GetXUserInfoAsync(
        string accessToken, CancellationToken ct)
    {
        var http = _httpFactory.CreateClient();

        // Request user.fields = profile_image_url,description
        var url = $"{XUserEndpoint}?user.fields=profile_image_url";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            _log.LogError("X /users/me failed: {Status} {Body}",
                response.StatusCode, err);
            throw new AppException(401, "X_AUTH",
                "Không lấy được thông tin user từ X");
        }

        var data = await response.Content
            .ReadFromJsonAsync<XUserApiResponse>(ct);

        if (data?.Data == null)
            throw new AppException(401, "X_AUTH", "Empty X user response");

        return new XUserInfo
        {
            XId = data.Data?.Id ?? "",
            Username = data.Data?.Username ?? "",
            Name = data.Data?.Name ?? "",
            ProfileImageUrl = data.Data?.ProfileImageUrl,
        };
    }

    private async Task<XLoginResponse> FindOrCreateUserAsync(
        XUserInfo xUser, CancellationToken ct)
    {
        var userRepo = _uow.Repository<User>();
        var isNew = false;

        // ── 1. Tìm theo XId ──
        var users = await userRepo.FindAsync(
            u => u.XId == xUser.XId, ct);
        var user = users.FirstOrDefault();

        // ── 2. Nếu chưa có, thử email (nếu X trả email) ──
        if (user is null && !string.IsNullOrEmpty(xUser.Email))
        {
            users = await userRepo.FindAsync(
                u => u.Email == xUser.Email.ToLowerInvariant(), ct);
            user = users.FirstOrDefault();

            // Link X account
            if (user is not null)
            {
                user.XId = xUser.XId;
                user.XUsername = xUser.Username;
                _log.LogInformation(
                    "Linked X to existing user: {Email}", xUser.Email);
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
                Email = xUser.EffectiveEmail.ToLowerInvariant(),
                FullName = xUser.EffectiveName,
                XId = xUser.XId,
                XUsername = xUser.Username,
                AvatarUrl = xUser.ProfileImageUrl,
                AuthProvider = "x",
                RoleId = viewerRole.Id,
                PasswordHash = "",
                IsActive = true,
            };

            await userRepo.AddAsync(user, ct);
            isNew = true;

            _log.LogInformation(
                "Created user from X: @{Username} (id={XId}) role={Role}",
                xUser.Username, xUser.XId, viewerRole.Name);
        }

        // ── 4. Kiểm tra active ──
        if (!user.IsActive)
            throw new AppException(403, "DISABLED", "Tài khoản đã bị vô hiệu hóa");

        // ── 5. Update avatar ──
        if (!string.IsNullOrEmpty(xUser.ProfileImageUrl))
            user.AvatarUrl = xUser.ProfileImageUrl;

        // ── 6. Update X username (có thể đổi) ──
        if (!string.IsNullOrEmpty(xUser.Username)
            && user.XUsername != xUser.Username)
            user.XUsername = xUser.Username;

        // ── 7. Save ──
        user.LastLoginAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);

        // ── 8. Generate JWT ──
        var role = await _uow.Repository<Role>()
            .GetByIdAsync(user.RoleId, ct);

        var jti = Guid.NewGuid().ToString();
        var accessToken = GenerateAccessToken(user, role!);

        var accessExpiry = int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60");
        var refreshExpiry = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");

        return new XLoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = "", // TODO: generate refresh token
            ExpiresIn = accessExpiry * 60,
            RefreshExpiresIn = refreshExpiry * 86400,
            User = new(user.Id, user.Email, user.FullName, role?.Name ?? ""),
            IsNewUser = isNew,
        };
    }
}