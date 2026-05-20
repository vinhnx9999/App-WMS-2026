using System.Text.Json.Serialization;

namespace WMS.Application.Auth.DTOs.Facebook.FbResponse;

public record FacebookGraphMeResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("picture")]
    public FacebookPictureData? Picture { get; init; }
}