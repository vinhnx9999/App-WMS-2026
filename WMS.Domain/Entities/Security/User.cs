using WMS.Domain.Common;

namespace WMS.Domain.Entities.Security;

public class User : BaseEntity
{
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public Guid RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    /// <summary>"local" | "google" | "facebook" | "x" | "microsoft" | "linkedin"</summary>
    public string AuthProvider { get; set; } = "local";

    /// <summary>Google account ID (sub claim)</summary>
    public string? GoogleId { get; set; }

    /// <summary>Facebook user ID</summary>
    public string? FacebookId { get; set; }
    public string? AvatarUrl { get; set; }

    /// <summary>X (Twitter) user ID — numeric string</summary>
    public string? XId { get; set; }

    /// <summary>X @handle</summary>
    public string? XUsername { get; set; }

    /// <summary>Microsoft OID subject identifier (oid claim)</summary>
    public string? MicrosoftId { get; set; }

    /// <summary>Azure AD tenant ID (tid claim)</summary>
    public string? MicrosoftTenantId { get; set; }
    /// <summary>LinkedIn member ID (sub claim from userinfo)</summary>
    public string? LinkedInId { get; set; }

    // Navigation
    public Role Role { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public ICollection<AuditLog> AuditLogs { get; set; } = [];

    public bool IsExternalAuth => AuthProvider != "local";
    public bool HasFacebook => !string.IsNullOrEmpty(FacebookId);
    public bool HasGoogle => !string.IsNullOrEmpty(GoogleId);
    public bool HasX => !string.IsNullOrEmpty(XId);
    public bool HasMicrosoft => !string.IsNullOrEmpty(MicrosoftId);
    public bool HasLinkedIn => !string.IsNullOrEmpty(LinkedInId);
}
