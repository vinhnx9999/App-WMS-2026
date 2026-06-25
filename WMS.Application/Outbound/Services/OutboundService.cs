using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Outbound.DTOs;
using WMS.Application.SignalR;
using WMS.Application.SignalR.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Outbound;
using WMS.Domain.Entities.Product;
using WMS.Domain.Entities.InventoryAggregateRoot;
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
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        var list = new List<OutboundOrderDto>();
        foreach (var order in orders)
        {
            var dto = await GetOutboundOrderDtoAsync(order.Id, ct);
            list.Add(dto);
        }
        return list;
    }

    public async Task<OutboundOrderDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await GetOutboundOrderDtoAsync(id, ct);
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

        // Validate stock, allocate stock, & calculate total
        var invRepo = _uow.Repository<InventoryItem>();
        decimal totalValue = 0;
        foreach (var item in order.Items)
        {
            var inv = await invRepo.GetByIdAsync(item.InventoryItemId, ct)
                ?? throw new AppException(404, "NOT_FOUND", $"Sản phẩm {item.InventoryItemId} không tồn tại");

            // Perform Domain stock allocation
            inv.Allocate(item.Quantity);

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

            // Decrease inventory using DeductShippedStock
            var invItem = await invRepo.GetByIdAsync(shipped.InventoryItemId, ct)
                ?? throw new AppException(404, "NOT_FOUND", $"Sản phẩm {shipped.InventoryItemId} không tồn tại");

            var oldQty = invItem.Quantity;
            var oldStatus = invItem.Status;

            invItem.DeductShippedStock(shipped.PickedQuantity, orderItem.Quantity);

            var sku = await _uow.Repository<Sku>().GetByIdAsync(invItem.SkuId, ct);
            var isLowStock = sku != null && invItem.Quantity <= sku.MinQuantity && invItem.Quantity > 0;

            // Push inventory change
            await _notifier.InventoryChangedAsync(new InventoryChangedData
            {
                ItemId = invItem.Id,
                Sku = sku?.SkuCode ?? "",
                Name = sku?.Name ?? "",
                OldQuantity = oldQty,
                NewQuantity = invItem.Quantity,
                OldStatus = oldStatus,
                NewStatus = invItem.Status,
                ZoneId = null,
                ChangeType = "outbound",
            });

            // Push stock alerts
            if (isLowStock)
            {
                await _notifier.LowStockAlertAsync(new StockAlertData
                {
                    ItemId = invItem.Id,
                    Sku = sku?.SkuCode ?? "",
                    Name = sku?.Name ?? "",
                    CurrentQuantity = invItem.Quantity,
                    MinQuantity = sku?.MinQuantity ?? 0,
                });
            }

            if (invItem.Status == ItemStatus.OutOfStock)
            {
                await _notifier.OutOfStockAlertAsync(new StockAlertData
                {
                    ItemId = invItem.Id,
                    Sku = sku?.SkuCode ?? "",
                    Name = sku?.Name ?? "",
                });
            }
        }

        await _uow.SaveChangesAsync(ct);

        // Push order status
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
        var order = await _uow.Repository<OutboundOrder>().Query()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId && !x.IsDeleted, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Đơn xuất không tồn tại");

        if (order.Status == OutboundStatus.Shipped || order.Status == OutboundStatus.Delivered)
            throw new AppException(400, "INVALID_STATE", "Không thể hủy đơn đã xuất hàng");

        order.Cancel();

        // Release stock allocations
        var invRepo = _uow.Repository<InventoryItem>();
        foreach (var item in order.Items)
        {
            var inv = await invRepo.GetByIdAsync(item.InventoryItemId, ct);
            if (inv != null)
            {
                inv.ReleaseAllocation(item.Quantity);
            }
        }

        await _uow.SaveChangesAsync(ct);
    }

    private async Task<string> GenerateShipmentNumber()
    {
        var count = await _uow.Repository<OutboundOrder>().CountAsync();
        return $"SHP-{count + 1:D4}";
    }

    private async Task<OutboundOrderDto> GetOutboundOrderDtoAsync(Guid orderId, CancellationToken ct)
    {
        var order = await _uow.Repository<OutboundOrder>().Query()
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Đơn xuất không tồn tại");

        var items = await (from item in _uow.Repository<OutboundItem>().Query()
                            where item.OutboundOrderId == orderId
                            join inv in _uow.Repository<InventoryItem>().Query() on item.InventoryItemId equals inv.Id
                            join sku in _uow.Repository<Sku>().Query() on inv.SkuId equals sku.Id
                            select new OutboundItemDto(
                                item.InventoryItemId,
                                sku.SkuCode ?? "",
                                sku.Name ?? "",
                                item.Quantity,
                                item.PickedQuantity,
                                item.Note ?? ""
                             ))
                            .ToListAsync(ct);

        return new OutboundOrderDto(
            order.Id,
            order.ShipmentNumber,
            order.Customer?.Name ?? "",
            order.Destination,
            order.ExpectedDelivery,
            order.Status,
            order.TotalValue,
            items.Count,
            items,
            order.CreatedAt
        );
    }
}