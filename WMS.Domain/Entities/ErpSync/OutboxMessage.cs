using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities.ErpSync;

public class OutboxMessage : BaseEntity
{
    public string MessageType { get; set; } = "";
    public string Payload { get; set; } = "";
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 5;
    public string? ErrorMessage { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
}
