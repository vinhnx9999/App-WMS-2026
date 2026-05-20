using System.Text.Json.Serialization;

namespace WMS.Application.Auth.DTOs.Twitter.XResponse;

public record XUserApiResponse
{
    [JsonPropertyName("data")]
    public XUserData? Data { get; init; }
}
