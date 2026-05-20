using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Web;
using WMS.Application.Auth.DTOs.Facebook;
using WMS.Application.Auth.DTOs.Facebook.FbResponse;
using WMS.Application.Auth.Interfaces;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.Security;
using WMS.Domain.Interfaces;

namespace WMS.Application.Auth.Services.AuthProvider;

public class FacebookAuthService(
    IUnitOfWork uow,
    IConfiguration config,
    IHttpClientFactory httpFactory,
    ILogger<FacebookAuthService> log) : BaseAuthService(config), IFacebookAuthService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly IConfiguration _config = config;
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILogger<FacebookAuthService> _log = log;

    public async Task<FacebookLoginResponse> LoginAsync(
        string accessToken, CancellationToken ct)
    {
        // ── Bước 1: Verify token + lấy user info từ Graph API ──
        var fbUser = await GetFacebookUserInfoAsync(accessToken, ct);

        _log.LogInformation(
            "Facebook token verified: id={Id} email={Email} name={Name}",
            fbUser.FacebookId, fbUser.Email, fbUser.Name);

        // ── Bước 2: Find or create WMS user ──
        return await FindOrCreateUserAsync(fbUser, ct);
    }

    public async Task<FacebookLoginResponse> LoginWithCodeAsync(
        string code, string redirectUri, CancellationToken ct)
    {
        // ── Bước 1: Exchange code → short-lived token ──
        var shortToken = await ExchangeCodeForTokenAsync(code, redirectUri, ct);

        // ── Bước 2: Exchange short-lived → long-lived token ──
        var longToken = await GetLongLivedTokenAsync(shortToken, ct);

        // ── Bước 3: Get user info ──
        var fbUser = await GetFacebookUserInfoAsync(longToken, ct);

        _log.LogInformation(
            "Facebook auth code verified: id={Id} email={Email}",
            fbUser.FacebookId, fbUser.Email);

        // ── Bước 4: Find or create user ──
        return await FindOrCreateUserAsync(fbUser, ct);
    }

    public string GetFacebookAuthUrl(string redirectUri, string? state = null)
    {
        var appId = _config["Facebook:AppId"]
            ?? throw new InvalidOperationException("Facebook:AppId not configured");

        var q = HttpUtility.ParseQueryString("");
        q["client_id"] = appId;
        q["redirect_uri"] = redirectUri;
        q["scope"] = "email,public_profile";
        q["response_type"] = "code";
        q["state"] = state ?? "/";

        return $"{FbAuthEndpoint}?{q}";
    }

    private async Task<FacebookUserInfo> GetFacebookUserInfoAsync(
        string accessToken, CancellationToken ct)
    {
        var http = _httpFactory.CreateClient();

        // ── Step 1: Verify token + get basic info ──
        var meUrl = $"{GraphApiBase}/me?fields=id,name,email,picture.type(large)&access_token={accessToken}";

        var response = await http.GetAsync(meUrl, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(ct);
            _log.LogWarning("Facebook Graph API error: {Status} {Body}",
                response.StatusCode, errBody);
            throw new AppException(401, "FB_AUTH",
                "Token Facebook không hợp lệ hoặc đã hết hạn");
        }

        var fbData = await response.Content
            .ReadFromJsonAsync<FacebookGraphMeResponse>(ct)
            ?? throw new AppException(401, "FB_AUTH", "Empty Facebook response");

        // ── Step 2: Debug token (optional — verify app_id matches) ──
        await VerifyTokenMetadataAsync(accessToken, ct);

        // ── Extract picture URL ──
        var pictureUrl = fbData.Picture?.Data?.Url;

        return new FacebookUserInfo
        {
            FacebookId = fbData?.Id ?? "",
            Email = fbData?.Email?.ToLowerInvariant() ?? "",
            Name = fbData?.Name ?? "",
            PictureUrl = pictureUrl,
            EmailVerified = !string.IsNullOrEmpty(fbData?.Email),
        };
    }

    private async Task VerifyTokenMetadataAsync(
        string accessToken, CancellationToken ct)
    {
        var appSecret = _config["Facebook:AppSecret"] ?? "";
        var appId = _config["Facebook:AppId"] ?? "";

        if (string.IsNullOrEmpty(appSecret)) return;

        var http = _httpFactory.CreateClient();

        // appsecret_proof = HMAC-SHA256(app_secret, access_token)
        using var hmac = new System.Security.Cryptography.HMACSHA256(
            Encoding.UTF8.GetBytes(appSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(accessToken));
        var proof = BitConverter.ToString(hash).Replace("-", "").ToLower();

        var debugUrl = $"{GraphApiBase}/debug_token?input_token={accessToken}&access_token={appId}|{appSecret}";

        try
        {
            var resp = await http.GetFromJsonAsync<FacebookDebugTokenResponse>(debugUrl, ct);

            if (resp?.Data == null || !resp.Data.IsValid)
            {
                _log.LogWarning("Facebook token is invalid or expired");
                throw new AppException(401, "FB_AUTH", "Token Facebook đã hết hạn");
            }

            if (resp.Data.AppId != appId)
            {
                _log.LogWarning("Facebook token app_id mismatch: {Got} vs {Expected}",
                    resp.Data.AppId, appId);
                throw new AppException(401, "FB_AUTH", "Token không thuộc WMS app");
            }
        }
        catch (AppException) { throw; }
        catch (Exception ex)
        {
            // Debug token endpoint có thể fail nếu app secret không đúng
            _log.LogWarning(ex, "Facebook debug_token failed — skipping verification");
        }
    }

    private async Task<string> ExchangeCodeForTokenAsync(
        string code, string redirectUri, CancellationToken ct)
    {
        var appId = _config["Facebook:AppId"]!;
        var appSecret = _config["Facebook:AppSecret"]!;

        var http = _httpFactory.CreateClient();
        var url = $"{FbTokenEndpoint}?client_id={appId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&client_secret={appSecret}&code={code}";

        var resp = await http.GetFromJsonAsync<FacebookTokenResponse>(url, ct);

        if (resp?.AccessToken == null)
        {
            _log.LogError("Facebook code exchange failed");
            throw new AppException(401, "FB_AUTH", "Không thể đổi code lấy token");
        }

        return resp.AccessToken;
    }

    private async Task<string> GetLongLivedTokenAsync(
        string shortToken, CancellationToken ct)
    {
        var appId = _config["Facebook:AppId"]!;
        var appSecret = _config["Facebook:AppSecret"]!;

        var http = _httpFactory.CreateClient();
        var url = $"{GraphApiBase}/oauth/access_token?grant_type=fb_exchange_token&client_id={appId}&client_secret={appSecret}&fb_exchange_token={shortToken}";

        var resp = await http.GetFromJsonAsync<FacebookTokenResponse>(url, ct);

        return resp?.AccessToken ?? shortToken; // fallback
    }

    private async Task<FacebookLoginResponse> FindOrCreateUserAsync(
        FacebookUserInfo fbUser, CancellationToken ct)
    {
        var userRepo = _uow.Repository<User>();
        var isNew = false;

        // ── 1. Tìm theo FacebookId ──
        var users = await userRepo.FindAsync(
            u => u.FacebookId == fbUser.FacebookId, ct);
        var user = users.FirstOrDefault();

        // ── 2. Nếu chưa có, tìm theo Email (chỉ khi có email thật) ──
        if (user is null && !string.IsNullOrEmpty(fbUser.Email))
        {
            users = await userRepo.FindAsync(
                u => u.Email == fbUser.Email.ToLowerInvariant(), ct);
            user = users.FirstOrDefault();

            // Link Facebook vào user hiện có
            if (user is not null)
            {
                user.FacebookId = fbUser.FacebookId;
                if (!string.IsNullOrEmpty(fbUser.PictureUrl))
                    user.AvatarUrl = fbUser.PictureUrl;
                _log.LogInformation(
                    "Linked Facebook to existing user: {Email}",
                    fbUser.Email);
            }
        }

        // ── 3. Nếu vẫn chưa có, tạo mới ──
        if (user is null)
        {
            var viewerRole = (await _uow.Repository<Role>()
                .FindAsync(r => r.Name == "viewer", ct))
                .FirstOrDefault()
                ?? throw new AppException(500, "SEED", "Role 'viewer' chưa tồn tại");

            var email = fbUser.EffectiveEmail;

            user = new User
            {
                Email = email.ToLowerInvariant(),
                FullName = fbUser.Name,
                FacebookId = fbUser.FacebookId,
                AvatarUrl = fbUser.PictureUrl,
                AuthProvider = "facebook",
                RoleId = viewerRole.Id,
                PasswordHash = "",
                IsActive = true,
            };

            await userRepo.AddAsync(user, ct);
            isNew = true;

            _log.LogInformation(
                "Created new user from Facebook: {Email} fbId={FbId} role={Role}",
                user.Email, fbUser.FacebookId, viewerRole.Name);
        }

        // ── 4. Kiểm tra active ──
        if (!user.IsActive)
            throw new AppException(403, "DISABLED", "Tài khoản đã bị vô hiệu hóa");

        // ── 5. Update avatar nếu Facebook mới hơn ──
        if (!string.IsNullOrEmpty(fbUser.PictureUrl)
            && user.AvatarUrl != fbUser.PictureUrl)
        {
            user.AvatarUrl = fbUser.PictureUrl;
        }

        // ── 6. Update last login ──
        user.LastLoginAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);

        // ── 7. Load role ──
        var role = await _uow.Repository<Role>()
            .GetByIdAsync(user.RoleId, ct);

        // ── 8. Generate WMS JWT ──
        var token = GenerateAccessToken(user, role!);
        var expiry = int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60");

        return new FacebookLoginResponse
        {
            AccessToken = token,
            ExpiresIn = expiry * 60,
            User = new(user.Id, user.Email, user.FullName, role?.Name ?? ""),
            IsNewUser = isNew,
        };
    }
}