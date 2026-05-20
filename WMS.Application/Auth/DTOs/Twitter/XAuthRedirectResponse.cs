namespace WMS.Application.Auth.DTOs.Twitter;

public record XAuthRedirectResponse
{
    public string AuthUrl { get; init; } = "";
    public string State { get; init; } = "";
}
