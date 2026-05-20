namespace WMS.Application.Auth.DTOs.UserProfile;

public record UserProfileDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = "";
    public string FullName { get; init; } = "";
    public string? AvatarUrl { get; init; }
    public string AuthProvider { get; init; } = "local";
    public string Role { get; init; } = "";
    public Dictionary<string, bool> Permissions { get; init; } = [];
    public bool IsActive { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }

    // External auth info
    public bool HasGoogle { get; init; }
    public bool HasFacebook { get; init; }
    public bool HasX { get; init; }
    public string? XUsername { get; init; }

    public bool HasMicrosoft { get; init; }
}