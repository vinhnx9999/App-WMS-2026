using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Web;
using WMS.Application.Auth.DTOs.LinkedIn;
using WMS.Application.Auth.Interfaces;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.Security;
using WMS.Domain.Interfaces;
using Role = WMS.Domain.Entities.Security.Role;

namespace WMS.Application.Auth.Services.AuthProvider;

public class LinkedInAuthService(
    IUnitOfWork uow,
    IConfiguration config,
    IHttpClientFactory httpFactory,
    ILogger<LinkedInAuthService> log) : BaseAuthService(config), ILinkedInAuthService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly IConfiguration _config = config;
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILogger<LinkedInAuthService> _log = log;

    public string GetAuthUrl(string redirectUri, string? state = null)
    {
        var clientId = _config["LinkedIn:ClientId"]
            ?? throw new InvalidOperationException("LinkedIn:ClientId not configured");

        var q = HttpUtility.ParseQueryString("");
        q["response_type"] = "code";
        q["client_id"] = clientId;
        q["redirect_uri"] = redirectUri;
        q["scope"] = "openid profile email";
        q["state"] = state ?? "/";

        var authUrl = $"{LiAuthEndpoint}?{q}";

        _log.LogInformation("LinkedIn auth URL generated");

        return authUrl;
    }

    public async Task<LinkedInLoginResponse> LoginWithCodeAsync(
        string code, string redirectUri, CancellationToken ct)
    {
        // ── Bước 1: Exchange code for access_token ──
        var accessToken = await ExchangeCodeAsync(code, redirectUri, ct);

        // ── Bước 2: Get user info from OIDC userinfo endpoint ──
        var liUser = await GetUserInfoAsync(accessToken, ct);

        _log.LogInformation(
            "LinkedIn user: sub={Sub} name={Name} email={Email}",
            liUser.Sub, liUser.Name, liUser.Email);

        // ── Bước 3: Find or create WMS user ──
        return await FindOrCreateUserAsync(liUser, ct);
    }

    private async Task<string> ExchangeCodeAsync(
        string code, string redirectUri, CancellationToken ct)
    {
        var clientId = _config["LinkedIn:ClientId"]!;
        var clientSecret = _config["LinkedIn:ClientSecret"]!;

        var http = _httpFactory.CreateClient();

        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
        };

        var response = await http.PostAsync(
            LiTokenEndpoint, new FormUrlEncodedContent(body), ct);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            _log.LogError("LinkedIn token exchange failed: {Status} {Body}",
                response.StatusCode, err);
            throw new AppException(401, "LINKEDIN_AUTH",
                "Không thể đổi code lấy token từ LinkedIn");
        }

        var tokenData = (await response.Content
            .ReadFromJsonAsync<LinkedInTokenResponse>(ct))!;

        if (string.IsNullOrEmpty(tokenData.AccessToken))
            throw new AppException(401, "LINKEDIN_AUTH",
                "LinkedIn không trả access_token");

        return tokenData.AccessToken;
    }

    private async Task<LinkedInUserInfo> GetUserInfoAsync(
        string accessToken, CancellationToken ct)
    {
        var http = _httpFactory.CreateClient();

        var request = new HttpRequestMessage(
            HttpMethod.Get, LiUserinfoEndpoint);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            _log.LogError("LinkedIn userinfo failed: {Status} {Body}",
                response.StatusCode, err);
            throw new AppException(401, "LINKEDIN_AUTH",
                "Không lấy được thông tin user từ LinkedIn");
        }

        var data = (await response.Content
            .ReadFromJsonAsync<LinkedInUserInfo>(ct))!;

        if (string.IsNullOrEmpty(data.Sub))
            throw new AppException(401, "LINKEDIN_AUTH",
                "LinkedIn userinfo không chứa sub");

        return data;
    }

    private async Task<LinkedInLoginResponse> FindOrCreateUserAsync(
        LinkedInUserInfo liUser, CancellationToken ct)
    {
        var userRepo = _uow.Repository<User>();
        var isNew = false;

        // ── 1. Tìm theo LinkedInId (sub) ──
        var users = await userRepo.FindAsync(
            u => u.LinkedInId == liUser.Sub, ct);
        var user = users.FirstOrDefault();

        // ── 2. Tìm theo Email ──
        if (user is null && !string.IsNullOrEmpty(liUser.Email))
        {
            users = await userRepo.FindAsync(
                u => u.Email == liUser.BestEmail.ToLowerInvariant(), ct);
            user = users.FirstOrDefault();

            // Link LinkedIn account
            if (user is not null)
            {
                user.LinkedInId = liUser.Sub;
                _log.LogInformation(
                    "Linked LinkedIn to existing user: {Email}",
                    liUser.Email);
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
                Email = liUser.BestEmail.ToLowerInvariant(),
                FullName = liUser.Name,
                LinkedInId = liUser.Sub,
                AvatarUrl = liUser.Picture,
                AuthProvider = "linkedin",
                RoleId = viewerRole.Id,
                PasswordHash = "",
                IsActive = true,
            };

            await userRepo.AddAsync(user, ct);
            isNew = true;

            _log.LogInformation(
                "Created user from LinkedIn: {Name} ({Email}) sub={Sub}",
                liUser.Name, liUser.Email, liUser.Sub);
        }

        // ── 4. Kiểm tra active ──
        if (!user.IsActive)
            throw new AppException(403, "DISABLED", "Tài khoản đã bị vô hiệu hóa");

        // ── 5. Update avatar ──
        if (!string.IsNullOrEmpty(liUser.Picture))
            user.AvatarUrl = liUser.Picture;

        // ── 6. Save ──
        user.LastLoginAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);

        // ── 7. Generate JWT ──
        var role = await _uow.Repository<Role>()
            .GetByIdAsync(user.RoleId, ct);

        var jti = Guid.NewGuid().ToString();
        var accessToken = GenerateAccessToken(user, role!, jti);

        var accessExpiry = int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60");
        var refreshExpiry = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");

        return new LinkedInLoginResponse
        {
            AccessToken = accessToken,
            ExpiresIn = accessExpiry * 60,
            RefreshExpiresIn = refreshExpiry * 86400,
            User = new(user.Id, user.Email, user.FullName, role?.Name ?? ""),
            IsNewUser = isNew,
        };
    }
}
