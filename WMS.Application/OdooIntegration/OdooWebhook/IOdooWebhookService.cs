using WMS.Infrastructure.ERPs.Odoo.OdooWebhook;

namespace WMS.Application.OdooIntegration.OdooWebhook;

public interface IOdooWebhookService
{
    /// <summary>
    /// Xử lý webhook event từ Odoo.
    /// Validate → Lưu DB → Xử lý nghiệp vụ → Trả response.
    /// </summary>
    Task<WebhookResponse> HandleAsync(
        OdooWebhookPayload payload,
        string? ipAddress,
        CancellationToken ct = default);
}
