namespace WMS.Application.Auth.DTOs.Token;

public record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = "";
}
