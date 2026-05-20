using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WMS.Application.OdooIntegration.OdooWebhook;
using WMS.Infrastructure.ERPs.Odoo.OdooWebhook;

namespace DP.AppWMS.ApiService.Controllers;

public class WebhooksController(
    IOdooWebhookService webhookService,
    IConfiguration config,
    ILogger<WebhooksController> log) : BaseController
{
    private readonly IOdooWebhookService _webhookService = webhookService;
    private readonly IConfiguration _config = config;
    private readonly ILogger<WebhooksController> _log = log;

    /// <summary>
    /// Nhận webhook từ Odoo.
    /// Code gửi POST lên endpoint này.
    /// Không cần JWT auth — dùng shared secret header.
    /// </summary>
    [HttpPost("odoo")]
    public async Task<IActionResult> HandleOdooWebhook(
        CancellationToken ct)
    {
        // ── Verify secret ──
        var secret = Request.Headers["X-Webhook-Secret"].FirstOrDefault();
        var expectedSecret = _config["Odoo:WebhookSecret"];

        if (string.IsNullOrEmpty(expectedSecret))
        {
            _log.LogError("Odoo:WebhookSecret not configured");
            return StatusCode(500, new { error = "Webhook not configured" });
        }

        if (secret != expectedSecret)
        {
            _log.LogWarning(
                "Invalid webhook secret from {IP}",
                HttpContext.Connection.RemoteIpAddress);
            return Unauthorized(new { error = "Invalid secret" });
        }

        // ── Read & parse body ──
        OdooWebhookPayload? payload;
        try
        {
            payload = await Request.ReadFromJsonAsync<OdooWebhookPayload>(ct);
        }
        catch (JsonException ex)
        {
            _log.LogWarning(ex, "Invalid JSON in webhook body");
            return BadRequest(new { error = "Invalid JSON" });
        }

        if (payload == null)
            return BadRequest(new { error = "Empty payload" });

        // ── Get client IP ──
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        _log.LogInformation(
            "Odoo webhook received: {Event} picking={Name} from {IP}",
            payload.Event, payload.PickingName, ipAddress);

        // ── Process ──
        var response = await _webhookService.HandleAsync(
            payload, ipAddress, ct);

        // Luôn trả 200 OK để Odoo không retry
        return Ok(response);
    }
}