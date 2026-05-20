namespace WMS.Application.Auth.DTOs.Facebook;

public record FacebookLoginRequest
{
    /// <summary>
    /// Facebook access_token từ FB.login() hoặc FB.getAccessToken().
    /// Đây là opaque token (không phải JWT).
    /// API sẽ gọi Graph API /me để verify và lấy user info.
    /// </summary>
    public string AccessToken { get; init; } = "";
}
