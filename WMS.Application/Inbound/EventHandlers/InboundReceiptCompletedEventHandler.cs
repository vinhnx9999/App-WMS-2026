using MediatR;
using WMS.Domain.Entities.InboundOrderHistoryAggregateRoot;
using WMS.Domain.Events;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Handlers;

public class CreateInboundReceiptEventHandler(
    IRepository<InboundOrderHistory> historyRepo,
    ICurrentUser currentUser) : INotificationHandler<CreateInboundReceiptEvent>
{
    public async Task Handle(CreateInboundReceiptEvent notification, CancellationToken ct)
    {
        var receipt = notification.Receipt;
        if (receipt.InboundOrderId == null) return;

        var history = new InboundOrderHistory(
            receipt.InboundOrderId.Value,
            receipt.Id,
            null,
            null,
            null,
            currentUser.Id,
            currentUser.Email ?? "System",
            "Receive",
            "Receipt_Completed",
            $"Completed inbound receipt {receipt.ReceiptNumber} at warehouse {receipt.WarehouseId}.");

        await historyRepo.AddAsync(history, ct);
    }
}
