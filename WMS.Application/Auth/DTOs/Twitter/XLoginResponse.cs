using WMS.Application.Auth.DTOs.UserProfile;

namespace WMS.Application.Auth.DTOs.Twitter;

/// <summary>
/// Response khi X login thành công.
/// </summary>
public record XLoginResponse
{
    public string AccessToken { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public int ExpiresIn { get; init; }
    public int RefreshExpiresIn { get; init; }
    public UserInfoDto User { get; init; } = null!;
    public bool IsNewUser { get; init; }
}
