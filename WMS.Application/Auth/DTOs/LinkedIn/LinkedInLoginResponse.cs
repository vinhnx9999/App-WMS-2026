using WMS.Application.Auth.DTOs.UserProfile;

namespace WMS.Application.Auth.DTOs.LinkedIn;

/// <summary>LinkedIn login success response.</summary>
public record LinkedInLoginResponse
{
    public string AccessToken { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public int ExpiresIn { get; init; }
    public int RefreshExpiresIn { get; init; }
    public UserInfoDto User { get; init; } = null!;
    public bool IsNewUser { get; init; }
}
