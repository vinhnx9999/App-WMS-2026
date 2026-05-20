using System.Text.Json.Serialization;

namespace WMS.Application.Auth.DTOs.Facebook.FbResponse;

public record FacebookDebugTokenResponse
{
    [JsonPropertyName("data")]
    public FacebookDebugTokenData? Data { get; init; }
}
