using System.Text.Json.Serialization;

namespace WMS.Application.Auth.DTOs.Facebook.FbResponse;

public record FacebookPictureInner
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }
}
