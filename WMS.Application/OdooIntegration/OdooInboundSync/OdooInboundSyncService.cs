using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.Common.Models;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Inbound;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.ERPs.Odoo.DataClient;
using WMS.Infrastructure.ERPs.Odoo.DataConfig;

namespace WMS.Application.OdooIntegration.OdooInboundSync;

public class OdooInboundSyncService(
    IOdooClient odoo,
    IUnitOfWork uow,
    IOptions<OdooConfig> config,
    ILogger<OdooInboundSyncService> log) : IOdooInboundSyncService
{
    private readonly IOdooClient _odoo = odoo;
    private readonly IUnitOfWork _uow = uow;
    private readonly OdooMappingConfig _mapping = config.Value.Mapping;
    private readonly ILogger<OdooInboundSyncService> _log = log;

    /// <summary>Pull incoming pickings từ Odoo → WMS InboundOrders</summary>
    public async Task<int> SyncInboundPickingsAsync(CancellationToken ct)
    {
        _log.LogInformation("Syncing inbound pickings from Odoo...");

        // Domain filter: incoming pickings that are ready to receive
        var domain = new List<object>
        {
            new object[] { "picking_type_id", "=", _mapping.IncomingPickingTypeId },
            new object[] { "state", "=", "assigned" },  // ready to receive
        };

        var fields = new string[]
        {
            "name", "origin", "partner_id",
            "scheduled_date", "state",
            "move_ids", "move_ids_without_package"
        };

        var pickings = await _odoo.SearchReadAsync(
            "stock.picking", domain, fields,
            limit: 100, ct: ct);

        int synced = 0;
        var orderRepo = _uow.Repository<InboundOrder>();
        var supplierRepo = _uow.Repository<Supplier>();
        var itemRepo = _uow.Repository<InventoryItem>();

        foreach (var picking in pickings)
        {
            var pickingName = picking["name"]?.ToString() ?? "";
            var odooId = (int)(picking["id"] ?? 0);

            // Skip if already synced
            if (await orderRepo.ExistsAsync(
                o => o.OrderNumber == pickingName))
                continue;

            // Parse partner
            var partnerName = ExtractMany2oneName(picking, "partner_id");
            var suppliers = await supplierRepo.FindAsync(
                s => s.Name == partnerName, ct);
            Supplier? supplier = suppliers.FirstOrDefault();

            if (supplier == null)
            {
                // Auto-create supplier from Odoo partner
                supplier = new Supplier { Name = partnerName };
                await supplierRepo.AddAsync(supplier, ct);
            }

            // Parse scheduled date
            DateOnly? scheduledDate = null;
            if (picking.TryGetValue("scheduled_date", out var dateVal)
                && dateVal is string dateStr)
            {
                if (DateTime.TryParse(dateStr, out var dt))
                    scheduledDate = DateOnly.FromDateTime(dt);
            }

            // Create WMS order
            var order = new InboundOrder
            {
                OrderNumber = pickingName,
                SupplierId = supplier.Id,
                ExpectedDate = scheduledDate,
                Status = InboundStatus.Pending,
            };

            await orderRepo.AddAsync(order, ct);
            synced++;

            _log.LogInformation(
                "Synced Odoo picking {Name} → WMS InboundOrder", pickingName);
        }

        await _uow.SaveChangesAsync(ct);

        _log.LogInformation(
            "Odoo inbound sync done: {Count} pickings", synced);
        return synced;
    }

    /// <summary>Confirm receipt in Odoo (button_validate)</summary>
    public async Task ConfirmReceiptAsync(
        Guid wmsOrderId, CancellationToken ct)
    {
        var order = await _uow.Repository<InboundOrder>().Query()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == wmsOrderId, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Đơn nhập không tồn tại");

        if (order.Status != InboundStatus.Completed)
            throw new AppException(400, "NOT_READY",
                "Đơn nhập chưa hoàn tất trong WMS");

        // Find Odoo picking ID by order number
        var pickings = await _odoo.SearchReadAsync(
            "stock.picking",
            [new object[] { "name", "=", order.OrderNumber }],
            ["id", "move_ids_without_package"],
            limit: 1, ct: ct);

        if (pickings.Count == 0)
            throw new AppException(404, "ODOO_NOT_FOUND",
                $"Odoo picking '{order.OrderNumber}' không tìm thấy");

        var pickingId = (int)(pickings[0]["id"] ?? 0);

        // Step 1: Get stock.move records for this picking
        var moveIds = ExtractIds(pickings[0], "move_ids_without_package");
        var moves = await _odoo.SearchReadAsync(
            "stock.move",
            [new object[] { "id", "in", moveIds }],
            ["id", "product_id", "move_line_ids"],
            ct: ct);

        // Step 2: Update move_line quantities (qty_done)
        foreach (var move in moves)
        {
            var moveLineIds = ExtractIds(move, "move_line_ids");
            if (moveLineIds.Count == 0) continue;

            var productCode = ExtractMany2oneName(move, "product_id");
            var wmsItem = order.Items.FirstOrDefault(i =>
            {
                var inv = _uow.Repository<InventoryItem>()
                    .GetByIdAsync(i.InventoryItemId).GetAwaiter().GetResult();
                return inv?.Sku?.SkuCode == productCode;
            });

            if (wmsItem == null) continue;

            // Update stock.move.line with received quantity
            await _odoo.WriteAsync("stock.move.line",
                [.. moveLineIds],
                new Dictionary<string, object>
                {
                    ["qty_done"] = wmsItem.ReceivedQuantity
                }, ct);
        }

        // Step 3: Validate the picking
        var result = await _odoo.ExecuteMethodAsync(
            "stock.picking", [pickingId],
            "button_validate", ct);

        _log.LogInformation(
            "Odoo GR confirmed for picking {Name} (ID={Id})",
            order.OrderNumber, pickingId);
    }

    // ── Helpers ──

    private static string ExtractMany2oneName(
        Dictionary<string, object?> record, string field)
    {
        if (record.TryGetValue(field, out var val) && val is List<object?> m2o
            && m2o.Count >= 2)
        {
            return m2o[1]?.ToString() ?? "";
        }
        return "";
    }

    private static List<int> ExtractIds(
        Dictionary<string, object?> record, string field)
    {
        if (record.TryGetValue(field, out var val)
            && val is List<object?> list)
        {
            return [.. list.Select(x => x is long l ? (int)l : 0).Where(x => x != 0)];
        }
        return [];
    }
}