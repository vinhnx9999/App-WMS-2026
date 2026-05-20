using WMS.Application.Auth.DTOs.Twitter;

namespace WMS.Application.Auth.Interfaces;

public interface IXAuthService
{
    /// <summary>
    /// Bước 1: Tạo authorization URL + PKCE challenge.
    /// Server lưu code_verifier vào Redis (key=state, TTL=5min).
    /// Trả URL để frontend redirect browser.
    /// </summary>
    Task<XAuthRedirectResponse> GetAuthUrlAsync(
        string redirectUri, CancellationToken ct = default);

    /// <summary>
    /// Bước 2: Exchange authorization code + PKCE verifier.
    /// Lấy access_token → GET /2/users/me → find/create user.
    /// </summary>
    Task<XLoginResponse> LoginWithCodeAsync(
        string code, string state, string redirectUri,
        CancellationToken ct = default);
}
