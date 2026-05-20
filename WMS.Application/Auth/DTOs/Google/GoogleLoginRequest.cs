namespace WMS.Application.Auth.DTOs.Google;

public record GoogleLoginRequest
{
    /// <summary>Google ID Token (JWT) từ Google Sign-In SDK</summary>
    public string IdToken { get; init; } = "";
}
