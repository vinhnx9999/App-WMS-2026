using FluentValidation;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Outbound.DTOs;
using WMS.Application.SignalR;
using WMS.Application.SignalR.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Outbound;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Application.Outbound.Services;

public class OutboundService(IUnitOfWork uow, ICurrentUser user, IDashboardNotifier notifier) : IOutboundService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ICurrentUser _user = user;
    private readonly IDashboardNotifier _notifier = notifier;

    public async Task<List<OutboundOrderDto>> GetListAsync(CancellationToken ct)
    {
        var orders = await _uow.Repository<OutboundOrder>().Query()
            .Include(x => x.Customer)
            .Include(x => x.Items).ThenInclude(i => i.InventoryItem)
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return [.. orders.Select(MapToDto)];
    }

    public async Task<OutboundOrderDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var order = await _uow.Repository<OutboundOrder>().Query()
            .Include(x => x.Customer)
            .Include(x => x.Items).ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Đơn xuất không tồn tại");

        return MapToDto(order);
    }

    public async Task<OutboundOrderDto> CreateAsync(
        CreateOutboundRequest request, CancellationToken ct)
    {
        var orderNumber = await GenerateShipmentNumber();

        var items = request.Items.Select(i => new OutboundItem
        {
            InventoryItemId = i.InventoryItemId,
            Quantity = i.Quantity,
        });

        var order = OutboundOrder.Create(
            tenantId: _user.TenantId,
            shipmentNumber: orderNumber,
            customerId: request.PartnerId,
            destination: request.Destination,
            expectedDelivery: request.ExpectedDelivery,
            notes: request.Notes ?? "",
            items: items
        );

        // Validate stock & calculate total
        var invRepo = _uow.Repository<InventoryItem>();
        decimal totalValue = 0;
        foreach (var item in order.Items)
        {
            var inv = await invRepo.GetByIdAsync(item.InventoryItemId, ct)
                ?? throw new AppException(404, "NOT_FOUND", $"Sản phẩm {item.InventoryItemId} không tồn tại");

            if (inv.Quantity < item.Quantity)
                throw new AppException(400, "INSUFFICIENT_STOCK",
                    $"Sản phẩm {inv.Sku} chỉ còn {inv.Quantity}, không đủ để xuất {item.Quantity}");

            totalValue += inv.UnitPrice * item.Quantity;
        }
        order.UpdateTotalValue(totalValue);

        await _uow.Repository<OutboundOrder>().AddAsync(order, ct);
        await _uow.SaveChangesAsync(ct);

        return await GetByIdAsync(order.Id, ct);
    }

    public async Task ShipAsync(Guid orderId, ShipOutboundRequest request, CancellationToken ct)
    {
        var order = await _uow.Repository<OutboundOrder>().Query()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId && !x.IsDeleted, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Đơn xuất không tồn tại");

        order.Ship();

        var invRepo = _uow.Repository<InventoryItem>();

        foreach (var shipped in request.Items)
        {
            var orderItem = order.Items
                .FirstOrDefault(x => x.InventoryItemId == shipped.InventoryItemId);
            if (orderItem is null) continue;


            orderItem.PickedQuantity = shipped.PickedQuantity;

            // Decrease inventory
            var invItem = await invRepo.GetByIdAsync(shipped.InventoryItemId, ct)
                ?? throw new AppException(404, "NOT_FOUND", $"Sản phẩm {shipped.InventoryItemId} không tồn tại");

            if (invItem.Quantity < shipped.PickedQuantity)
                throw new AppException(400, "INSUFFICIENT_STOCK",
                    $"Tồn kho {invItem.Sku} chỉ còn {invItem.Quantity}");

            var oldQty = invItem.Quantity;
            var oldStatus = invItem.Status;

            invItem.Quantity -= shipped.PickedQuantity;
            invItem.UpdateStatus();

            // ── Push inventory change ──
            await _notifier.InventoryChangedAsync(new InventoryChangedData
            {
                ItemId = invItem.Id,
                Sku = invItem.SkuCode ?? "",
                Name = invItem.Name ?? "",
                OldQuantity = oldQty,
                NewQuantity = invItem.Quantity,
                OldStatus = oldStatus,
                NewStatus = invItem.Status,
                ZoneId = invItem.ZoneId,
                ChangeType = "outbound",
            });

            // ── Push stock alerts ──
            if (invItem.Status == ItemStatus.LowStock)
                await _notifier.LowStockAlertAsync(new StockAlertData
                {
                    ItemId = invItem.Id,
                    Sku = invItem.SkuCode ?? "",
                    Name = invItem.Name ?? "",
                    CurrentQuantity = invItem.Quantity,
                    MinQuantity = invItem.MinQuantity,
                });

            if (invItem.Status == ItemStatus.OutOfStock)
                await _notifier.OutOfStockAlertAsync(new StockAlertData
                {
                    ItemId = invItem.Id,
                    Sku = invItem.SkuCode ?? "",
                    Name = invItem.Name ?? "",
                });

        }

        // status transition is already done via order.Ship()
        await _uow.SaveChangesAsync(ct);

        // ── Push order status ──
        await _notifier.OutboundStatusChangedAsync(new OrderStatusChangedData
        {
            OrderId = order.Id,
            OrderNumber = order.ShipmentNumber,
            OldStatus = "Picking",
            NewStatus = "Shipped",
            ItemsCount = order.Items.Count,
        });
    }

    public async Task CancelAsync(Guid orderId, CancellationToken ct)
    {
        var order = await _uow.Repository<OutboundOrder>().GetByIdAsync(orderId, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Đơn xuất không tồn tại");

        order.Cancel();
        await _uow.SaveChangesAsync(ct);
    }

    private async Task<string> GenerateShipmentNumber()
    {
        var count = await _uow.Repository<OutboundOrder>().CountAsync();
        return $"SHP-{count + 1:D4}";
    }

    private static OutboundOrderDto MapToDto(OutboundOrder o) => new(
        o.Id, o.ShipmentNumber, o.Customer?.Name ?? "",
        o.Destination, o.ExpectedDelivery, o.Status, o.TotalValue,
        o.Items.Count,
        [.. o.Items.Select(i => new OutboundItemDto(
            i.InventoryItemId,
            i.InventoryItem?.Sku?.SkuCode ?? "",
            i.InventoryItem?.Name ?? "",
            i.Quantity, i.PickedQuantity, i.Note ?? ""))],
        o.CreatedAt);
}