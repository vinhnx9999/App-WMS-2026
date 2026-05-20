using System.Text.Json.Serialization;

namespace WMS.Application.Auth.DTOs.Facebook.FbResponse;

public record FacebookDebugTokenData
{
    [JsonPropertyName("app_id")]
    public string? AppId { get; init; }

    [JsonPropertyName("is_valid")]
    public bool IsValid { get; init; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; init; }

    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; init; }

    [JsonPropertyName("scopes")]
    public string[]? Scopes { get; init; }
}