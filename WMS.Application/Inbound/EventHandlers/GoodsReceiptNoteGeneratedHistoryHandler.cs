using MediatR;
using WMS.Domain.Entities.InboundOrderHistoryAggregateRoot;
using WMS.Domain.Events;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Handlers;

public class GoodsReceiptNoteGeneratedHistoryHandler(
    IRepository<InboundOrderHistory> historyRepo,
    ICurrentUser currentUser) : INotificationHandler<GoodsReceiptNoteGeneratedEvent>
{
    public async Task Handle(GoodsReceiptNoteGeneratedEvent notification, CancellationToken ct)
    {
        var grn = notification.Grn;
        if (grn.InboundOrderId == null) return;

        var history = new InboundOrderHistory(
            grn.InboundOrderId.Value,
            grn.InboundReceiptId,
            null,
            grn.PutawayTaskId,
            grn.Id,
            currentUser.Id,
            currentUser.Email ?? "System",
            "GRN",
            "GRN_Generated",
            $"Generated goods receipt note {grn.GrnNumber} for warehouse {grn.WarehouseId}.");

        await historyRepo.AddAsync(history, ct);
    }
}
