using MediatR;
using WMS.Domain.Entities.InboundOrderHistoryAggregateRoot;
using WMS.Domain.Events;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Handlers;

public class PutawayTaskCompletedHistoryHandler(
    IRepository<InboundOrderHistory> historyRepo,
    ICurrentUser currentUser) : INotificationHandler<PutawayTaskCompletedEvent>
{
    public async Task Handle(PutawayTaskCompletedEvent notification, CancellationToken ct)
    {
        var task = notification.Task;
        if (task.InboundOrderId == null)
        {
            return;
        }

        var history = new InboundOrderHistory(
            task.InboundOrderId.Value,
            task.InboundReceiptId,
            task.QcInspectionId,
            task.Id,
            null,
            currentUser.Id,
            currentUser.Email ?? "System",
            "Putaway",
            "Putaway_Finished",
            $"Completed putaway task {task.PutawayTaskNumber} at warehouse {task.WarehouseId}.");

        await historyRepo.AddAsync(history, ct);
    }
}
