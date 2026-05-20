namespace WMS.Application.Auth.DTOs.Google;

public record GoogleTokenResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("id_token")]
    public string IdToken { get; init; } = "";

    [System.Text.Json.Serialization.JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("token_type")]
    public string TokenType { get; init; } = "";

    [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }
}