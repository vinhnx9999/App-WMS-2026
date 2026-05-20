using WMS.Application.Auth.DTOs.LinkedIn;

namespace WMS.Application.Auth.Interfaces;

public interface ILinkedInAuthService
{
    /// <summary>
    /// Bước 1: Build LinkedIn authorization URL.
    /// </summary>
    string GetAuthUrl(string redirectUri, string? state = null);

    /// <summary>
    /// Bước 2: Exchange code → get userinfo → find/create user → JWT.
    /// </summary>
    Task<LinkedInLoginResponse> LoginWithCodeAsync(
        string code, string redirectUri, CancellationToken ct = default);
}