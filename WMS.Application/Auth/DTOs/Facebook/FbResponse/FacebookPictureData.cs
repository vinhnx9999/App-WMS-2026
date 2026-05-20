using System.Text.Json.Serialization;

namespace WMS.Application.Auth.DTOs.Facebook.FbResponse;

public record FacebookPictureData
{
    [JsonPropertyName("data")]
    public FacebookPictureInner? Data { get; init; }
}
