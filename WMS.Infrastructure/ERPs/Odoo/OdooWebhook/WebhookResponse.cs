namespace WMS.Infrastructure.ERPs.Odoo.OdooWebhook;

/// <summary>
/// Response trả về cho Odoo sau khi nhận webhook
/// </summary>
public record WebhookResponse
{
    public bool Received { get; init; }
    public string? EventId { get; init; }
    public string? Message { get; init; }
}