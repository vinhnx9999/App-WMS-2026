using System.Text.Json.Serialization;

namespace WMS.Application.Auth.DTOs.LinkedIn;

/// <summary>LinkedIn user info from OIDC /v2/userinfo.</summary>
public record LinkedInUserInfo
{
    public string Sub { get; init; } = "";         // Unique LinkedIn member ID
    public string Name { get; init; } = "";         // Full name
    public string? GivenName { get; init; }         // First name
    public string? FamilyName { get; init; }        // Last name
    public string? Email { get; init; }
    public bool EmailVerified { get; init; }
    public string? Picture { get; init; }           // Avatar URL
    public string? Locale { get; init; }

    /// <summary>Effective email with fallback.</summary>
    public string BestEmail =>
        !string.IsNullOrEmpty(Email) ? Email
        : $"linkedin_{Sub}@linkedin.wms.local";
}

public record LinkedInTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; init; }

    [JsonPropertyName("id_token")]
    public string? IdToken { get; init; }
}