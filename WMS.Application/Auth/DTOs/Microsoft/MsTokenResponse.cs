using System.Text.Json.Serialization;

namespace WMS.Application.Auth.DTOs.Microsoft;

public record MsTokenResponse
{
    [JsonPropertyName("id_token")]
    public string IdToken { get; init; } = "";

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; init; }
}
