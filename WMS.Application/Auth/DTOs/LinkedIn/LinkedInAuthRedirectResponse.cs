namespace WMS.Application.Auth.DTOs.LinkedIn;

/// <summary>LinkedIn authorization URL response.</summary>
public record LinkedInAuthRedirectResponse
{
    public string AuthUrl { get; init; } = "";
}
