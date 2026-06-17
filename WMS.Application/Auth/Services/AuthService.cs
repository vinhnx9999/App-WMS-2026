using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using WMS.Application.Auth.DTOs.Login;
using WMS.Application.Auth.DTOs.Logout;
using WMS.Application.Auth.DTOs.Token;
using WMS.Application.Auth.DTOs.UserProfile;
using WMS.Application.Auth.Interfaces;
using WMS.Application.Common.Models;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Security;
using WMS.Domain.Interfaces;
using Role = WMS.Domain.Entities.Security.Role;

namespace WMS.Application.Auth.Services;

public class AuthService(
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ITokenRevocationStore revocationStore,
    IConfiguration config,
    ILogger<AuthService> log) :
    BaseAuthService(config), IAuthService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly ITokenRevocationStore _revocationStore = revocationStore;
    private readonly IConfiguration _config = config;
    private readonly ILogger<AuthService> _log = log;

    public async Task<LoginResponse> LoginAsync(
        LoginRequest req, CancellationToken ct)
    {
        // Find user
        var users = await _uow.Repository<User>()
            .FindAsync(u => u.Email == req.Email && !u.IsDeleted, ct);
        var user = users.FirstOrDefault()
            ?? throw new AppException(401, "AUTH", "Email hoặc mật khẩu không đúng");

        if (user.IsExternalAuth)
            throw new AppException(401, "AUTH",
                "Tài khoản đăng nhập bằng Google/Facebook. Hãy dùng phương thức tương ứng.");

        if (!user.IsActive)
            throw new AppException(403, "DISABLED", "Tài khoản đã bị vô hiệu hóa");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new AppException(401, "AUTH", "Email hoặc mật khẩu không đúng");

        var role = await _uow.Repository<Role>().GetByIdAsync(user.RoleId, ct);

        user.LastLoginAt = DateTime.UtcNow;

        // Generate tokens
        var jti = Guid.NewGuid().ToString();
        var accessToken = GenerateAccessToken(user, role!, jti);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id, jti, ct);

        await _uow.SaveChangesAsync(ct);

        var accessExpiry = int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60");
        var refreshExpiry = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresIn = accessExpiry * 60,
            RefreshExpiresIn = refreshExpiry * 86400,
            User = new(user.Id, user.Email, user.FullName, role?.Name ?? ""),
        };
    }

    public async Task<UserProfileDto> GetProfileAsync(CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new AppException(401, "UNAUTHORIZED", "Chưa đăng nhập");

        var user = await _uow.Repository<User>().Query()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == _currentUser.Id && !u.IsDeleted, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Người dùng không tồn tại");

        if (!user.IsActive)
            throw new AppException(403, "DISABLED", "Tài khoản đã bị vô hiệu hóa");

        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            AuthProvider = user.AuthProvider,
            Role = user.Role?.Name ?? "",
            Permissions = user.Role?.Permissions ?? [],
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            HasGoogle = user.HasGoogle,
            HasFacebook = user.HasFacebook,
        };
    }

    public async Task LogoutAsync(LogoutRequest req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new AppException(401, "UNAUTHORIZED", "Chưa đăng nhập");

        var userId = _currentUser.Id;
        var currentJti = _currentUser.Jti;

        _log.LogInformation("Logout: userId={UserId} jti={Jti} allDevices={All}",
            userId, currentJti, req.LogoutAllDevices);

        // ── Bước 1: Revoke current JWT (thêm vào Redis blocklist) ──
        if (!string.IsNullOrEmpty(currentJti))
        {
            // TTL = thời gian còn lại của token
            // (token có thể đã expired — vẫn revoke để chắc)
            var tokenRemaining = _currentUser.TokenRemainingTime;
            var ttl = tokenRemaining > TimeSpan.Zero
                ? tokenRemaining
                : TimeSpan.FromMinutes(5); // Fallback: giữ 5 phút

            await _revocationStore.RevokeAsync(currentJti, ttl);
        }

        // ── Bước 2: Revoke refresh token ──
        if (!string.IsNullOrEmpty(req.RefreshToken))
        {
            var refreshTokenHash = GenerateCodeChallenge(req.RefreshToken);
            var tokens = await _uow.Repository<RefreshToken>()
                .FindAsync(
                    t => t.UserId == userId
                      && t.Token == refreshTokenHash
                      && t.IsActive,
                    ct);

            var token = tokens.FirstOrDefault();
            if (token is not null)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = _currentUser.IpAddress;
            }
        }

        // ── Bước 3: Logout all devices? ──
        if (req.LogoutAllDevices)
        {
            var allTokens = await _uow.Repository<RefreshToken>()
                .FindAsync(t => t.UserId == userId && t.IsActive, ct);

            foreach (var t in allTokens)
            {
                t.IsRevoked = true;
                t.RevokedAt = DateTime.UtcNow;
                t.RevokedByIp = _currentUser.IpAddress;

                // Revoke associated JWT too
                if (!string.IsNullOrEmpty(t.Jti))
                    await _revocationStore.RevokeAsync(t.Jti, TimeSpan.FromDays(1));
            }

            _log.LogInformation("All devices logged out: userId={UserId} count={Count}",
                userId, allTokens.Count);
        }

        // ── Bước 4: Audit log ──
        await _uow.Repository<AuditLog>().AddAsync(new AuditLog
        {
            UserId = userId,
            Action = req.LogoutAllDevices ? "LOGOUT_ALL" : "LOGOUT",
            TableName = "User",
            EntityId = userId,
            IpAddress = _currentUser.IpAddress,
        }, ct);

        await _uow.SaveChangesAsync(ct);

        _log.LogInformation("Logout completed: userId={UserId}", userId);
    }

    public async Task<TokenResponse> RefreshTokenAsync(
        RefreshTokenRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken))
            throw new AppException(400, "INVALID_TOKEN", "Thiếu refresh token");

        var tokenHash = GenerateCodeChallenge(req.RefreshToken);

        // ── Find refresh token ──
        var tokens = await _uow.Repository<RefreshToken>().Query()
            .Include(t => t.User)
            .ThenInclude(u => u.Role)
            .Where(t => t.Token == tokenHash)
            .ToListAsync(ct);

        var refreshToken = tokens.FirstOrDefault()
            ?? throw new AppException(401, "INVALID_TOKEN", "Refresh token không hợp lệ");

        // ── Validate ──
        if (refreshToken.IsRevoked)
        {
            // SECURITY: Token đã bị revoke nhưng vẫn được dùng
            // → Revoke toàn bộ family (token reuse detection)
            _log.LogWarning(
                "Refresh token reuse detected! userId={UserId} — revoking all tokens",
                refreshToken.UserId);

            await RevokeAllUserTokensAsync(refreshToken.UserId, ct);
            throw new AppException(401, "TOKEN_REUSE",
                "Refresh token đã bị thu hồi. Đăng nhập lại trên tất cả thiết bị.");
        }

        if (refreshToken.IsExpired)
            throw new AppException(401, "TOKEN_EXPIRED", "Refresh token đã hết hạn");

        var user = refreshToken.User;
        if (!user.IsActive)
            throw new AppException(403, "DISABLED", "Tài khoản đã bị vô hiệu hóa");

        // ── Rotation: revoke old → issue new ──
        var newJti = Guid.NewGuid().ToString();
        var newAccessToken = GenerateAccessToken(user, user.Role, newJti);
        var newRefreshToken = await GenerateRefreshTokenAsync(
            user.Id, newJti, ct);

        // Revoke old refresh token
        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.ReplacedByToken = newRefreshToken.Token; // Rotation chain

        // Revoke old JWT
        if (!string.IsNullOrEmpty(refreshToken.Jti))
        {
            var oldTokenRemaining = refreshToken.ExpiresAt - DateTime.UtcNow;
            await _revocationStore.RevokeAsync(
                refreshToken.Jti,
                oldTokenRemaining > TimeSpan.Zero ? oldTokenRemaining : TimeSpan.FromMinutes(5));
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);

        var accessExpiry = int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60");
        var refreshExpiry = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");

        _log.LogInformation("Token refreshed: userId={UserId} oldJti={OldJti} newJti={NewJti}",
            user.Id, refreshToken.Jti, newJti);

        return new TokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresIn = accessExpiry * 60,
            RefreshExpiresIn = refreshExpiry * 86400,
        };
    }

    protected async Task<RefreshToken> GenerateRefreshTokenAsync(
        Guid userId, string jti, CancellationToken ct)
    {
        // Generate cryptographically random token
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var tokenString = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var refreshDays = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");

        var entity = new RefreshToken
        {
            UserId = userId,
            Token = GenerateCodeChallenge(tokenString),
            Jti = jti,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
            CreatedByIp = _currentUser.IpAddress ?? "",
        };

        await _uow.Repository<RefreshToken>().AddAsync(entity, ct);

        // Cleanup: xóa expired/revoked tokens của user (giữ DB sạch)
        var oldTokens = await _uow.Repository<RefreshToken>()
            .FindAsync(
                t => t.UserId == userId
                  && (t.IsRevoked || t.ExpiresAt < DateTime.UtcNow),
                ct);

        foreach (var old in oldTokens.Take(50)) // Limit để không block
            await _uow.Repository<RefreshToken>().DeleteAsync(old);

        // Return raw token (không hash) — chỉ trả 1 lần
        var toReturn = new RefreshToken
        {
            UserId = entity.UserId,
            Token = tokenString,     // Raw token cho client
            Jti = entity.Jti,
            ExpiresAt = entity.ExpiresAt,
        };

        return toReturn;
    }


    protected async Task RevokeAllUserTokensAsync(
        Guid userId, CancellationToken ct)
    {
        var allTokens = await _uow.Repository<RefreshToken>()
            .FindAsync(t => t.UserId == userId && t.IsActive, ct);

        foreach (var t in allTokens)
        {
            t.IsRevoked = true;
            t.RevokedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(t.Jti))
                await _revocationStore.RevokeAsync(t.Jti, TimeSpan.FromDays(1));
        }
    }
}
