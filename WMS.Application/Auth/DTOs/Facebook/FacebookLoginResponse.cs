using WMS.Application.Auth.DTOs.UserProfile;

namespace WMS.Application.Auth.DTOs.Facebook;

/// <summary>
/// Response khi Facebook login thành công.
/// Cấu trúc giống hệt GoogleLoginResponse để frontend thống nhất.
/// </summary>
public record FacebookLoginResponse
{
    public string AccessToken { get; init; } = "";   // WMS JWT
    public int ExpiresIn { get; init; }
    public UserInfoDto User { get; init; } = null!;
    public bool IsNewUser { get; init; }
}
