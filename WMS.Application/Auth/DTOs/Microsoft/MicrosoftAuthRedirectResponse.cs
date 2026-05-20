namespace WMS.Application.Auth.DTOs.Microsoft;

public record MicrosoftAuthRedirectResponse
{
    public string AuthUrl { get; init; } = "";
}
