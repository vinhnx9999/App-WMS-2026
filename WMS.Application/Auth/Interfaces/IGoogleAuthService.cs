using WMS.Application.Auth.DTOs.Google;

namespace WMS.Application.Auth.Interfaces;

public interface IGoogleAuthService
{
    /// <summary>
    /// Flow B: Verify Google ID Token → find/create user → return WMS JWT.
    /// </summary>
    Task<GoogleLoginResponse> LoginWithIdTokenAsync(
        string idToken, CancellationToken ct = default);

    /// <summary>
    /// Flow A: Exchange authorization code for tokens → get user info.
    /// </summary>
    Task<GoogleLoginResponse> LoginWithAuthCodeAsync(
        string code, string redirectUri, CancellationToken ct = default);

    /// <summary>
    /// Get Google OAuth URL for redirect flow.
    /// </summary>
    string GetGoogleAuthUrl(string redirectUri, string? state = null);
}
