using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Inbound.DTOs;
using WMS.Application.SignalR;
using WMS.Application.SignalR.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Entities.Master;
using WMS.Domain.Entities.Outbound;
using WMS.Domain.Entities.InventoryAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using WMS.Domain.Entities.SkuAggregateRoot;

using WMS.Domain.Entities.InboundReceiptAggregateRoot;
using WMS.Domain.Entities.QcInspectionAggregateRoot;
using WMS.Domain.Entities.PutawayTaskAggregateRoot;
using WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;
using WMS.Domain.Entities.ProductAggregateRoot;

namespace WMS.Application.Inbound.Services;

public class InboundService(IUnitOfWork uow, ICurrentUser user, IDashboardNotifier notifier) : IInboundService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ICurrentUser _user = user;
    private readonly IDashboardNotifier _notifier = notifier;

    public async Task<InboundOrderDto?> CreateAsync(CreateInboundRequest req, CancellationToken ct)
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
                SkuId = i.SkuId,
                Quantity = i.Quantity,
            })]
        };

        // Calculate total value
        var skuRepo = _uow.Repository<Sku>();
        foreach (var item in order.Items)
        {
            var sku = await skuRepo.GetByIdAsync(item.SkuId, ct);
            if (sku != null)
                order.TotalValue += (sku.ReferencePrice ?? 0) * item.Quantity;
        }

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

        return await GetInboundOrderDtoAsync(order.Id, ct);
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
                .FirstOrDefault(x => x.SkuId == received.SkuId);
            if (orderItem is null) continue;

            orderItem.ReceivedQuantity = received.ReceivedQuantity;
            orderItem.Note = received.Note;

            // Increase inventory
            await UpdateInventoryStock(
                received.SkuId,
                received.ReceivedQuantity, ct);
        }

        order.Status = InboundStatus.Completed;
        order.ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow);

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

    private async Task<InboundOrderDto> GetInboundOrderDtoAsync(Guid orderId, CancellationToken ct)
    {
        var order = await _uow.Repository<InboundOrder>().GetByIdAsync(orderId, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Đơn nhập không tồn tại");

        var supplier = await _uow.Repository<Supplier>().GetByIdAsync(order.SupplierId, ct);
        var supplierName = supplier?.Name ?? "";

        var items = await (from item in _uow.Repository<InboundItem>().Query()
                            where item.InboundOrderId == orderId
                            join sku in _uow.Repository<Sku>().Query() on item.SkuId equals sku.Id
                            select new InboundItemDto(
                                sku.SkuCode ?? "",
                                sku.Name ?? "",
                                item.Quantity,
                                item.ReceivedQuantity
                            ))
                           .ToListAsync(ct);

        return new InboundOrderDto(
            order.Id,
            order.OrderNumber,
            supplierName,
            order.ExpectedDate,
            order.Status,
            order.TotalValue,
            items.Count,
            items
        );
    }

    public async Task<List<InboundOrderDto>> GetListAsync(CancellationToken ct)
    {
        var orders = await _uow.Repository<InboundOrder>().Query()
            .ToListAsync(ct);

        var list = new List<InboundOrderDto>();
        foreach (var order in orders)
        {
            var dto = await GetInboundOrderDtoAsync(order.Id, ct);
            list.Add(dto);
        }
        return list;
    }

    public async Task CancelAsync(Guid id, CancellationToken ct)
    {
        var repo = _uow.Repository<InboundOrder>();
        var order = await repo.GetByIdAsync(id, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Đơn nhập không tồn tại");

        if (order.Status == InboundStatus.Completed)
            throw new AppException(400, "ALREADY_COMPLETED", "Không thể hủy đơn nhập đã hoàn thành");

        order.Status = InboundStatus.Cancelled;
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<InboundOrderDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await GetInboundOrderDtoAsync(id, ct);
    }
}