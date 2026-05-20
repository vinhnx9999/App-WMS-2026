namespace WMS.Application.Auth.DTOs.Twitter;

/// <summary>
/// X user info từ X API v2 /2/users/me.
/// </summary>
public record XUserInfo
{
    public string XId { get; init; } = "";         // Numeric ID
    public string Username { get; init; } = "";     // @handle
    public string Name { get; init; } = "";         // Display name
    public string? ProfileImageUrl { get; init; }

    /// <summary>X API có thể trả email (hiếm khi).</summary>
    public string? Email { get; init; }

    /// <summary>Fallback email nếu X không trả.</summary>
    public string EffectiveEmail =>
        !string.IsNullOrEmpty(Email)
            ? Email
            : $"x_{XId}@x.wms.local";

    /// <summary>Display name fallback.</summary>
    public string EffectiveName =>
        !string.IsNullOrEmpty(Name) ? Name : $"@{Username}";
}