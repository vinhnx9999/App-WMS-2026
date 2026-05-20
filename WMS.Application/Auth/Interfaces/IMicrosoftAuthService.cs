using WMS.Application.Auth.DTOs.Microsoft;

namespace WMS.Application.Auth.Interfaces;

public interface IMicrosoftAuthService
{
    /// <summary>
    /// Bước 1: Build Microsoft authorization URL.
    /// Redirect browser đến Microsoft login.
    /// </summary>
    string GetAuthUrl(string redirectUri, string? state = null);

    /// <summary>
    /// Bước 2: Exchange authorization code → verify id_token
    /// → find/create user → return WMS JWT.
    /// </summary>
    Task<MicrosoftLoginResponse> LoginWithCodeAsync(
        string code, string redirectUri, CancellationToken ct = default);
}