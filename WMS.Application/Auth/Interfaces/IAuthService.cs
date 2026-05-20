using WMS.Application.Auth.DTOs.Login;
using WMS.Application.Auth.DTOs.Logout;
using WMS.Application.Auth.DTOs.Token;
using WMS.Application.Auth.DTOs.UserProfile;

namespace WMS.Application.Auth.Interfaces;

public interface IAuthService
{
    Task<UserProfileDto> GetProfileAsync(CancellationToken ct = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    /// <summary>
    /// Logout — revoke current JWT + refresh token.
    /// </summary>
    Task LogoutAsync(LogoutRequest req, CancellationToken ct = default);

    /// <summary>
    /// Refresh — đổi refresh_token lấy access_token mới.
    /// Implements refresh token rotation (old token revoked, new token issued).
    /// </summary>
    Task<TokenResponse> RefreshTokenAsync(
        RefreshTokenRequest req, CancellationToken ct = default);
}
