using WMS.Application.Auth.DTOs.UserProfile;

namespace WMS.Application.Auth.DTOs.Login;

public record LoginResponse
{
    public string AccessToken { get; init; } = null!;
    public string RefreshToken { get; init; } = null!;
    public int ExpiresIn { get; init; }
    public UserInfoDto User { get; init; } = null!;
    public int RefreshExpiresIn { get; internal set; }
}
