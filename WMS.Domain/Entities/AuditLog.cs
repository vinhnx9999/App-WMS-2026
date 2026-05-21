using WMS.Domain.Common;

namespace WMS.Domain.Entities;

// Entity AuditLog
public class AuditLog : BaseEntity
{
    public string TableName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string KeyValues { get; set; } = string.Empty;
    public string OldValues { get; set; } = string.Empty;
    public string NewValues { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid UserId { get; set; }
    public Guid? EntityId { get; set; }
    public string? IpAddress { get; set; } = string.Empty;
}
