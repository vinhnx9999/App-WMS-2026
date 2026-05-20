namespace WMS.Infrastructure.ERPs.Odoo.OdooWebhook;

public record OdooWebhookPayload
{
    public string Event { get; init; } = "";              // "picking.validated"
    public int PickingId { get; init; }                    // Odoo stock.picking.id
    public string PickingName { get; init; } = "";         // e.g. "WH/IN/00042"
    public string PickingType { get; init; } = "";         // "incoming" | "outgoing"
    public string State { get; init; } = "";               // "done"
    public string? Partner { get; init; }                  // Partner name
    public string? Origin { get; init; }                   // PO ref / SO ref
    public string? Timestamp { get; init; }                // ISO datetime
}
