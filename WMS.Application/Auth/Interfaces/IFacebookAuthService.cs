using WMS.Application.Auth.DTOs.Facebook;

namespace WMS.Application.Auth.Interfaces;

public interface IFacebookAuthService
{
    /// <summary>
    /// Verify Facebook access_token → Graph API /me → find/create user → WMS JWT.
    /// </summary>
    Task<FacebookLoginResponse> LoginAsync(
        string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Exchange authorization code for access_token (server-side flow).
    /// </summary>
    Task<FacebookLoginResponse> LoginWithCodeAsync(
        string code, string redirectUri, CancellationToken ct = default);

    /// <summary>
    /// Build Facebook OAuth URL for redirect flow.
    /// </summary>
    string GetFacebookAuthUrl(string redirectUri, string? state = null);
}