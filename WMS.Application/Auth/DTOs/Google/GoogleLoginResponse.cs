using WMS.Application.Auth.DTOs.UserProfile;

namespace WMS.Application.Auth.DTOs.Google;

/// <summary>
/// Response trả về khi login Google thành công.
/// Giống hệt LoginResponse để frontend xử lý thống nhất.
/// </summary>
public record GoogleLoginResponse
{
    public string AccessToken { get; init; } = "";
    public int ExpiresIn { get; init; }
    public UserInfoDto User { get; init; } = null!;
    public bool IsNewUser { get; init; }
}
