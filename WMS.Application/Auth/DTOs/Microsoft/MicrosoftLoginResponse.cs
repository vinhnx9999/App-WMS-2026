using WMS.Application.Auth.DTOs.UserProfile;

namespace WMS.Application.Auth.DTOs.Microsoft;

/// <summary>
/// Response khi Microsoft login thành công.
/// </summary>
public record MicrosoftLoginResponse
{
    public string AccessToken { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public int ExpiresIn { get; init; }
    public int RefreshExpiresIn { get; init; }
    public UserInfoDto User { get; init; } = null!;
    public bool IsNewUser { get; init; }
}
