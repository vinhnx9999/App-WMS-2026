using WMS.Application.Common.Models;
using WMS.Application.Inbound.DTOs;
using WMS.Application.SignalR;
using WMS.Application.SignalR.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Inbound;
using WMS.Domain.Entities.Outbound;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Services;

public class InboundService(IUnitOfWork uow, ICurrentUser user, IDashboardNotifier notifier) : IInboundService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ICurrentUser _user = user;
    private readonly IDashboardNotifier _notifier = notifier;

    public async Task<InboundOrderDto> CreateAsync(CreateInboundRequest req, CancellationToken ct)
    {
        var order = new InboundOrder
        {
            OrderNumber = await GenerateOrderNumber(),
            SupplierId = req.SupplierId,
            ExpectedDate = req.ExpectedDate,
            Notes = req.Notes,
            Status = InboundStatus.Pending,
            Items = [.. req.Items.Select(i => new InboundItem
            {
                InventoryItemId = i.InventoryItemId,
                Quantity = i.Quantity,
            })]
        };

        // Calculate total value
        var invRepo = _uow.Repository<InventoryItem>();
        foreach (var item in order.Items)
        {
            var inv = await invRepo.GetByIdAsync(item.InventoryItemId, ct);
            if (inv != null)
                order.TotalValue += inv.UnitPrice * item.Quantity;
        }

        await _uow.Repository<InboundOrder>().AddAsync(order, ct);
        await _uow.SaveChangesAsync(ct);

        // ── Push SignalR event ──
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

        return MapToDto(order);
    }

    // ═══ Helper — push dashboard summary ═══
    private async Task PushDashboardSummaryAsync(CancellationToken ct)
    {
        var totalItems = await _uow.Repository<InventoryItem>().CountAsync();
        var lowStock = await _uow.Repository<InventoryItem>()
            .CountAsync(x => x.Status == ItemStatus.LowStock);
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

        var repo = _uow.Repository<InboundOrder>();
        var order = await repo.GetByIdAsync(orderId, ct)
            ?? throw new AppException(404, "NOT_FOUND",
                "Đơn nhập không tồn tại");

        if (order.Status == InboundStatus.Completed)
            throw new AppException(400, "ALREADY_COMPLETED",
                "Đơn nhập đã hoàn tất");

        // Update received quantities
        foreach (var received in req.Items)
        {
            var orderItem = order.Items
                .FirstOrDefault(x => x.InventoryItemId == received.InventoryItemId);
            if (orderItem is null) continue;

            orderItem.ReceivedQuantity = received.ReceivedQuantity;
            orderItem.Note = received.Note;

            // Increase inventory
            await UpdateInventoryStock(
                received.InventoryItemId,
                received.ReceivedQuantity, ct);
        }

        order.Status = InboundStatus.Completed;
        order.ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow);

        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task UpdateInventoryStock(
        Guid itemId, int qty, CancellationToken ct)
    {
        var item = await _uow.Repository<InventoryItem>()
            .GetByIdAsync(itemId, ct)
            ?? throw new AppException(404, "NOT_FOUND",
                $"Sản phẩm {itemId} không tồn tại");

        item.Quantity += qty;
        item.UpdateStatus();
    }

    private async Task<string> GenerateOrderNumber()
    {
        var count = await _uow.Repository<InboundOrder>()
            .CountAsync();
        return $"PO-{DateTime.UtcNow:yyyy}-{(count + 1):D4}";
    }

    private static InboundOrderDto MapToDto(InboundOrder o) => new(
        o.Id, o.OrderNumber, o.Supplier?.Name ?? "",
        o.ExpectedDate, o.Status, o.TotalValue,
        o.Items.Count,
        [.. o.Items.Select(i => new InboundItemDto(
            i.InventoryItem?.Sku?.SkuCode ?? "",
            i.InventoryItem?.Sku?.Name ?? "",
            i.Quantity, i.ReceivedQuantity))]);

    public Task<List<InboundOrderDto>> GetListAsync(CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task CancelAsync(Guid id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<InboundOrderDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}