using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.Common.Models;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Outbound;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.ERPs.Odoo.DataClient;
using WMS.Infrastructure.ERPs.Odoo.DataConfig;

namespace WMS.Application.OdooIntegration.OdooOutboundSync
{
    public class OdooOutboundSyncService(
        IOdooClient odoo, IUnitOfWork uow,
        IOptions<OdooConfig> config,
        ILogger<OdooOutboundSyncService> log) : IOdooOutboundSyncService
    {
        private readonly IOdooClient _odoo = odoo;
        private readonly IUnitOfWork _uow = uow;
        private readonly OdooMappingConfig _mapping = config.Value.Mapping;
        private readonly ILogger<OdooOutboundSyncService> _log = log;

        /// <summary>Pull outgoing deliveries từ Odoo</summary>
        public async Task<int> SyncOutboundDeliveriesAsync(
            CancellationToken ct)
        {
            _log.LogInformation("Syncing outbound deliveries from Odoo...");

            var domain = new List<object>
            {
                new object[] { "picking_type_id", "=", _mapping.OutgoingPickingTypeId },
                new object[] { "state", "=", "assigned" },
            };

            var pickings = await _odoo.SearchReadAsync(
                "stock.picking", domain,
                [ "name", "origin", "partner_id",
                         "scheduled_date", "state" ],
                limit: 100, ct: ct);

            int synced = 0;
            var orderRepo = _uow.Repository<OutboundOrder>();
            var partnerRepo = _uow.Repository<Customer>();

            foreach (var picking in pickings)
            {
                var name = picking["name"]?.ToString() ?? "";

                if (await orderRepo.ExistsAsync(o => o.ShipmentNumber == name))
                    continue;

                var partnerName = ExtractMany2oneName(picking, "partner_id");
                var partners = await partnerRepo.FindAsync(
                    p => p.Name == partnerName, ct);
                var partner = partners.FirstOrDefault();

                if (partner == null)
                {
                    partner = new Customer { Name = partnerName, Type = "Customer" };
                    await partnerRepo.AddAsync(partner, ct);
                }

                DateOnly? deliveryDate = null;
                if (picking.TryGetValue("scheduled_date", out var d)
                    && d is string ds && DateTime.TryParse(ds, out var dt))
                {
                    deliveryDate = DateOnly.FromDateTime(dt);
                }

                var origin = picking.TryGetValue("origin", out var o)
                    ? o?.ToString() : null;

                var order = new OutboundOrder
                {
                    ShipmentNumber = name,
                    CustomerId = partner.Id,
                    Destination = partnerName,
                    ExpectedDelivery = deliveryDate,
                    Notes = $"Odoo Origin: {origin}",
                    Status = OutboundStatus.Pending,
                };

                await orderRepo.AddAsync(order, ct);
                synced++;

                _log.LogInformation(
                    "Synced Odoo delivery {Name} → WMS OutboundOrder", name);
            }

            await _uow.SaveChangesAsync(ct);
            _log.LogInformation(
                "Odoo outbound sync done: {Count} deliveries", synced);
            return synced;
        }

        /// <summary>Confirm delivery in Odoo</summary>
        public async Task ConfirmDeliveryAsync(
            Guid wmsOrderId, CancellationToken ct)
        {
            var order = await _uow.Repository<OutboundOrder>().Query()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == wmsOrderId, ct)
                ?? throw new AppException(404, "NOT_FOUND", "Đơn xuất không tồn tại");

            if (order.Status != OutboundStatus.Shipped)
                throw new AppException(400, "NOT_READY",
                    "Đơn xuất chưa shipped trong WMS");

            // Find Odoo picking
            var pickings = await _odoo.SearchReadAsync(
                "stock.picking",
                [
                    new object[] { "name", "=", order.ShipmentNumber }
                ],
                ["id", "move_ids_without_package"],
                limit: 1, ct: ct);

            if (pickings.Count == 0)
                throw new AppException(404, "ODOO_NOT_FOUND",
                    $"Odoo picking '{order.ShipmentNumber}' không tìm thấy");

            var pickingId = (int)(pickings[0]["id"] ?? 0);

            // Update move lines with picked quantities
            var moveIds = ExtractIds(pickings[0], "move_ids_without_package");
            var moves = await _odoo.SearchReadAsync(
                "stock.move",
                [new object[] { "id", "in", moveIds }],
                ["id", "product_id", "move_line_ids"],
                ct: ct);

            foreach (var move in moves)
            {
                var lineIds = ExtractIds(move, "move_line_ids");
                if (lineIds.Count == 0) continue;

                var productCode = ExtractMany2oneName(move, "product_id");
                var wmsItem = order.Items.FirstOrDefault(i =>
                {
                    var inv = _uow.Repository<InventoryItem>()
                        .GetByIdAsync(i.InventoryItemId).GetAwaiter().GetResult();
                    return inv?.Sku?.SkuCode == productCode;
                });

                if (wmsItem == null) continue;

                await _odoo.WriteAsync("stock.move.line",
                    lineIds.ToArray(),
                    new Dictionary<string, object>
                    {
                        ["qty_done"] = wmsItem.PickedQuantity
                    }, ct);
            }

            // Validate
            await _odoo.ExecuteMethodAsync(
                "stock.picking", [pickingId],
                "button_validate", ct);

            _log.LogInformation(
                "Odoo delivery confirmed: {Name}", order.ShipmentNumber);
        }

        private static string ExtractMany2oneName(
            Dictionary<string, object?> record, string field)
        {
            if (record.TryGetValue(field, out var val) && val is List<object?> m2o
                && m2o.Count >= 2)
                return m2o[1]?.ToString() ?? "";
            return "";
        }

        private static List<int> ExtractIds(
            Dictionary<string, object?> record, string field)
        {
            if (record.TryGetValue(field, out var val) && val is List<object?> list)
                return [.. list.Select(x => x is long l ? (int)l : 0).Where(x => x > 0)];
            return [];
        }
    }
}
