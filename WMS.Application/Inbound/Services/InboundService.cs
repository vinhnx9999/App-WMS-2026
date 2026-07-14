using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Inbound.DTOs;
using WMS.Application.SignalR;
using WMS.Application.SignalR.DTOs;
using WMS.Domain.Common;
using WMS.Domain.Entities;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Entities.InventoryAggregateRoot;
using WMS.Domain.Entities.Outbound;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Services;

public class InboundService(IUnitOfWork uow, ICurrentUser user, IDashboardNotifier notifier) : IInboundService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ICurrentUser _user = user;
    private readonly IDashboardNotifier _notifier = notifier;

    public async Task<InboundOrderDto?> CreateAsync(CreateInboundRequest req, CancellationToken ct)
    {
        var order = InboundOrder.Create(
            _user.TenantId,
            await GenerateOrderNumber(),
            req.ExpectedDate,
            req.Notes);

        foreach (var item in req.Items)
        {
            order.AddItem(item.SkuId, item.Quantity, item.SupplierId);
        }

        // Calculate total value
        var skuRepo = _uow.Repository<Sku>();
        decimal totalValue = 0;
        foreach (var item in order.Items)
        {
            var sku = await skuRepo.GetByIdAsync(item.SkuId, ct);
            if (sku != null)
                totalValue += (sku.ReferencePrice ?? 0) * item.Quantity;
        }
        order.SetTotalValue(totalValue);

        await _uow.Repository<InboundOrder>().AddAsync(order, ct);
        await _uow.SaveChangesAsync(ct);

        // Push SignalR event
        if (_notifier != null)
        {
            await _notifier.InboundStatusChangedAsync(new OrderStatusChangedData
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                OldStatus = "",
                NewStatus = order.Status.ToString(),
                PartnerName = "",
                ItemsCount = order.Items.Count,
            });

            await PushDashboardSummaryAsync(ct);
        }

        return new InboundOrderDto(
            order.Id,
            order.OrderNumber,
            order.ExpectedDate,
            order.Status,
            order.TotalValue,
            order.Items.Count
        );
    }

    private async Task PushDashboardSummaryAsync(CancellationToken ct)
    {
        var totalItems = await _uow.Repository<InventoryItem>().CountAsync();

        var lowStock = await (from item in _uow.Repository<InventoryItem>().Query()
                              join sku in _uow.Repository<Sku>().Query() on item.SkuId equals sku.Id
                              where item.Quantity <= sku.MinQuantity && item.Quantity > 0 && !item.IsDeleted
                              select item.Id)
                             .CountAsync(ct);

        var outOfStock = await _uow.Repository<InventoryItem>()
            .CountAsync(x => x.Status == ItemStatus.OutOfStock);

        var pendingIn = await _uow.Repository<InboundOrder>()
            .CountAsync(x => x.Status != InboundStatus.Completed
                           && x.Status != InboundStatus.Cancelled);

        var pendingOut = await _uow.Repository<OutboundOrder>()
            .CountAsync(x => x.Status != OutboundStatus.Shipped
                           && x.Status != OutboundStatus.Delivered
                           && x.Status != OutboundStatus.Cancelled);

        await _notifier.DashboardSummaryChangedAsync(new DashboardSummaryData
        {
            TotalItems = totalItems,
            LowStockCount = lowStock,
            OutOfStockCount = outOfStock,
            PendingInbound = pendingIn,
            PendingOutbound = pendingOut,
            ConnectedClients = await _notifier.GetConnectedClientsAsync(),
        });
    }

    public async Task ReceiveAsync(
        Guid orderId, ReceiveInboundRequest req,
        CancellationToken ct)
    {
        await using var tx = await _uow.BeginTransactionAsync(ct);

        var order = await _uow.Repository<InboundOrder>().Query()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId && !x.IsDeleted, ct)
            ?? throw new AppException(404, "NOT_FOUND",
                "Đơn nhập không tồn tại");

        if (order.Status == InboundStatus.Completed)
            throw new AppException(400, "ALREADY_COMPLETED",
                "Đơn nhập đã hoàn tất");

        // Update received quantities
        foreach (var received in req.Items)
        {
            order.ReceiveItem(received.SkuId, received.ReceivedQuantity, received.Note);

            // Increase inventory
            await UpdateInventoryStock(
                received.SkuId,
                received.ReceivedQuantity, ct);
        }

        order.CompleteOrder();

        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task UpdateInventoryStock(
        Guid skuId, int qty, CancellationToken ct)
    {
        var item = await _uow.Repository<InventoryItem>().GetByIdAsync(skuId, ct);
        if (item == null)
        {
            var items = await _uow.Repository<InventoryItem>()
                .FindAsync(x => x.SkuId == skuId && !x.IsDeleted, ct);
            item = items.FirstOrDefault();
        }

        if (item != null)
        {
            item.AddStock(qty);
        }
        else
        {
            var location = await _uow.Repository<LocationEntity>().Query().FirstOrDefaultAsync(x => !x.IsDeleted, ct);
            var locationId = location?.Id ?? Guid.Empty;
            var newItem = InventoryItem.Create(
                Guid.Empty,
                skuId,
                locationId,
                null,
                null,
                null,
                qty,
                0,
                DateTime.UtcNow,
                null
            );
            await _uow.Repository<InventoryItem>().AddAsync(newItem, ct);
        }
    }

    private async Task<string> GenerateOrderNumber()
    {
        var count = await _uow.Repository<InboundOrder>()
            .CountAsync();
        return $"PO-{DateTime.UtcNow:yyyy}-{(count + 1):D4}";
    }

    public async Task CancelAsync(Guid id, CancellationToken ct)
    {
        var repo = _uow.Repository<InboundOrder>();
        var order = await repo.GetByIdAsync(id, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Đơn nhập không tồn tại");

        try
        {
            order.Cancel();
        }
        catch (DomainException ex)
        {
            throw new AppException(400, ex.Code, ex.Message);
        }
        await _uow.SaveChangesAsync(ct);
    }
}