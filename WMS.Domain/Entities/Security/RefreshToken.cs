using WMS.Domain.Common;

namespace WMS.Domain.Entities.Security;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = "";         // Hashed refresh token
    public string Jti { get; set; } = "";            // Associated JWT jti
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }     // Token rotation chain
    public string CreatedByIp { get; set; } = "";
    public string? UserAgent { get; set; }

    public User User { get; set; } = null!;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}