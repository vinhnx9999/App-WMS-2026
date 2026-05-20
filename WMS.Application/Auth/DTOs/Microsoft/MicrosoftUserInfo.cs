namespace WMS.Application.Auth.DTOs.Microsoft;
/// <summary>
/// Microsoft user info từ id_token claims.
/// </summary>
public record MicrosoftUserInfo
{
    public string Oid { get; init; } = "";          // "oid" claim — unique user ID
    public string Email { get; init; } = "";         // "email" claim
    public string Name { get; init; } = "";          // "name" claim
    public string? PreferredUsername { get; init; }   // "preferred_username"
    public string? TenantId { get; init; }           // "tid" claim
    public string? Picture { get; init; }

    /// <summary>Preferred email: email claim → preferred_username fallback</summary>
    public string BestEmail =>
        !string.IsNullOrEmpty(Email) ? Email
        : PreferredUsername ?? $"ms_{Oid}@microsoft.wms.local";
}
