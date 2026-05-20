namespace WMS.Application.Auth.DTOs.Token;

public record TokenResponse
{
    public string AccessToken { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public int ExpiresIn { get; init; }
    public int RefreshExpiresIn { get; init; }
}