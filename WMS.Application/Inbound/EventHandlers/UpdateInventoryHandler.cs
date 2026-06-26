using MediatR;
using WMS.Domain.Entities.InventoryAggregateRoot;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Interfaces;
using WMS.Domain.Events;

namespace WMS.Application.Inbound.Handlers;

public class UpdateInventoryHandler(
    IRepository<InventoryItem> inventoryRepo,
    IRepository<InboundOrder> inboundOrderRepo,
    ICurrentUser currentUser) : INotificationHandler<PutawayTaskCompletedEvent>
{
    public async Task Handle(PutawayTaskCompletedEvent notification, CancellationToken ct)
    {
        Guid? supplierId = null;
        if (notification.Task.InboundOrderId.HasValue)
        {
            var inboundOrder = await inboundOrderRepo.GetByIdAsync(notification.Task.InboundOrderId.Value, ct);
            if (inboundOrder != null)
            {
                supplierId = inboundOrder.SupplierId;
            }
        }

        foreach (var item in notification.Task.Items)
        {
            if (item.ActualLocationId == null)
            {
                continue;
            }

            var matchingItems = await inventoryRepo.FindAsync(x =>
                x.SkuId == item.SkuId &&
                x.LocationId == item.ActualLocationId.Value &&
                x.SupplierId == supplierId &&
                x.SerialNumber == null &&
                x.PalletId == null &&
                x.ExpiryDate == null, ct);

            var existingItem = matchingItems.FirstOrDefault();

            if (existingItem != null)
            {
                existingItem.AddStock(item.PutawayQuantity);
                await inventoryRepo.UpdateAsync(existingItem);
            }
            else
            {
                var newItem = InventoryItem.Create(
                    currentUser.TenantId,
                    item.SkuId,
                    item.ActualLocationId.Value,
                    supplierId,
                    null,
                    null,
                    item.PutawayQuantity,
                    0m,
                    DateTime.UtcNow,
                    null);
                await inventoryRepo.AddAsync(newItem, ct);
            }
        }
    }
}
