using System.Text.Json.Serialization;

namespace WMS.Application.Auth.DTOs.Twitter.XResponse;

public record XUserData
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("profile_image_url")]
    public string? ProfileImageUrl { get; init; }
}