using Microsoft.Extensions.Logging;
using System.Text.Json;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.ErpSync;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Entities.Outbound;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.ERPs.Odoo.OdooWebhook;

namespace WMS.Application.OdooIntegration.OdooWebhook;

public class OdooWebhookService(
    IUnitOfWork uow,
    ILogger<OdooWebhookService> log) : IOdooWebhookService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ILogger<OdooWebhookService> _log = log;

    public async Task<WebhookResponse> HandleAsync(
        OdooWebhookPayload payload,
        string? ipAddress,
        CancellationToken ct)
    {
        // ── Bước 1: Validate payload ──
        if (string.IsNullOrWhiteSpace(payload.Event))
            throw new AppException(400, "INVALID_PAYLOAD", "Thiếu trường 'event'");

        if (string.IsNullOrWhiteSpace(payload.PickingName))
            throw new AppException(400, "INVALID_PAYLOAD", "Thiếu trường 'picking_name'");

        // ── Bước 2: Tạo idempotency key ──
        var idempotencyKey = $"odoo:{payload.PickingId}:{payload.Event}";

        // ── Bước 3: Kiểm tra duplicate ──
        var webhookRepo = _uow.Repository<WebhookEvent>();
        var exists = await webhookRepo.ExistsAsync(
            w => w.IdempotencyKey == idempotencyKey
              && w.Status == "Completed");

        if (exists)
        {
            _log.LogInformation(
                "Duplicate webhook ignored: {Key}", idempotencyKey);
            return new WebhookResponse
            {
                Received = true,
                Message = "Already processed (duplicate)"
            };
        }

        // ── Bước 4: Lưu webhook event ──
        var webhookEvent = new WebhookEvent
        {
            Source = "Odoo",
            EventType = payload.Event,
            Payload = JsonSerializer.Serialize(payload),
            Status = "Processing",
            IdempotencyKey = idempotencyKey,
            IpAddress = ipAddress,
        };

        await webhookRepo.AddAsync(webhookEvent, ct);

        // ── Bước 5: Xử lý theo event type ──
        try
        {
            switch (payload.Event)
            {
                case "picking.validated":
                    await HandlePickingValidatedAsync(payload, ct);
                    break;

                case "purchase.order.confirmed":
                    await HandlePurchaseOrderConfirmedAsync(payload, ct);
                    break;

                case "stock.quant.updated":
                    await HandleStockQuantUpdatedAsync(payload, ct);
                    break;

                default:
                    _log.LogWarning(
                        "Unknown Odoo webhook event: {Event}", payload.Event);
                    break;
            }

            webhookEvent.Status = "Completed";
            webhookEvent.ProcessedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync(ct);

            _log.LogInformation(
                "Webhook processed: {Event} picking={Name} ({Type})",
                payload.Event, payload.PickingName, payload.PickingType);

            return new WebhookResponse
            {
                Received = true,
                EventId = webhookEvent.Id.ToString(),
                Message = $"Processed: {payload.Event}"
            };
        }
        catch (Exception ex)
        {
            webhookEvent.Status = "Failed";
            webhookEvent.ErrorMessage = ex.Message;
            webhookEvent.RetryCount++;

            _log.LogError(ex,
                "Webhook processing failed: {Event} picking={Name}",
                payload.Event, payload.PickingName);

            // Vẫn lưu — không throw để Odoo nhận 200 OK
            await _uow.SaveChangesAsync(ct);

            return new WebhookResponse
            {
                Received = true,
                EventId = webhookEvent.Id.ToString(),
                Message = "Received but processing failed — queued for retry"
            };
        }
    }

    // ── Xử lý picking.validated ──
    private async Task HandlePickingValidatedAsync(
        OdooWebhookPayload payload, CancellationToken ct)
    {
        _log.LogInformation(
            "Handling picking.validated: {Name} type={Type}",
            payload.PickingName, payload.PickingType);

        // Tìm WMS order tương ứng
        if (payload.PickingType == "incoming")
        {
            var order = (await _uow.Repository<InboundOrder>()
                .FindAsync(o => o.OrderNumber == payload.PickingName, ct))
                .FirstOrDefault();

            if (order != null)
            {
                _log.LogInformation(
                    "Odoo picking {Name} → WMS InboundOrder {Id} already synced",
                    payload.PickingName, order.Id);

                // Nếu WMS order chưa complete, đánh dấu Odoo đã confirm
                if (order.Status != InboundStatus.Completed)
                {
                    order.Status = InboundStatus.Completed;
                    order.ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow);
                    // await _uow.SaveChangesAsync(ct);
                }
            }
            else
            {
                _log.LogWarning(
                    "Odoo picking {Name} not found in WMS inbound orders",
                    payload.PickingName);
            }
        }
        else if (payload.PickingType == "outgoing")
        {
            var order = (await _uow.Repository<OutboundOrder>()
                .FindAsync(o => o.ShipmentNumber == payload.PickingName, ct))
                .FirstOrDefault();

            if (order != null)
            {
                _log.LogInformation(
                    "Odoo picking {Name} → WMS OutboundOrder {Id} confirmed",
                    payload.PickingName, order.Id);

                if (order.Status != OutboundStatus.Shipped
                    && order.Status != OutboundStatus.Delivered)
                {
                    order.Ship();
                    //  await _uow.SaveChangesAsync(ct);
                }
            }
            else
            {
                _log.LogWarning(
                    "Odoo picking {Name} not found in WMS outbound orders",
                    payload.PickingName);
            }
        }
    }

    // ── Xử lý purchase.order.confirmed ──
    private async Task HandlePurchaseOrderConfirmedAsync(
        OdooWebhookPayload payload, CancellationToken ct)
    {
        _log.LogInformation(
            "Handling purchase.order.confirmed: origin={Origin}",
            payload.Origin);

        // Trigger immediate PO sync từ Odoo
        // (background job sẽ pick up nếu cần)
    }

    // ── Xử lý stock.quant.updated ──
    private async Task HandleStockQuantUpdatedAsync(
        OdooWebhookPayload payload, CancellationToken ct)
    {
        _log.LogInformation(
            "Handling stock.quant.updated: picking={Name}",
            payload.PickingName);

        // Có thể trigger immediate stock reconciliation
    }
}