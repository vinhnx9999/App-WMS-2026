using WMS.Domain.Common;

namespace WMS.Domain.Entities.ErpSync;

public class ErpSyncLog : BaseEntity
{
    public string Provider { get; set; } = "";       // SAP | Odoo
    public string Direction { get; set; } = "";      // INBOUND | OUTBOUND
    public string EntityType { get; set; } = "";     // "PO", "GR", "GI", "MATERIAL"
    public Guid? WmsEntityId { get; set; }           // WMS Order ID
    public string? ErpDocNumber { get; set; }        // Doc #
    public string Status { get; set; } = "Pending";  // SUCCESS, FAILED, PENDING
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
}


public class WebhookEvent : BaseEntity
{
    public string Source { get; set; } = "";         // "Odoo" | "SAP"
    public string EventType { get; set; } = "";      // "picking.validated", "po.confirmed"
    public string Payload { get; set; } = "";        // Raw JSON
    public string Status { get; set; } = "Pending";  // Pending | Processing | Completed | Failed
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 5;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public string? IdempotencyKey { get; set; }      // Dedup: picking_id + event
    public string? IpAddress { get; set; }
}
