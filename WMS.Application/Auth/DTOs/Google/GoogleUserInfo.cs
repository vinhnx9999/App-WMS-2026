namespace WMS.Application.Auth.DTOs.Google;

// ═══ Internal: Google user info từ token verification ═══

public record GoogleUserInfo
{
    public string GoogleId { get; init; } = "";   // sub
    public string Email { get; init; } = "";
    public string Name { get; init; } = "";       // full_name
    public string? Picture { get; init; }        // avatar URL
    public bool EmailVerified { get; init; }
}
