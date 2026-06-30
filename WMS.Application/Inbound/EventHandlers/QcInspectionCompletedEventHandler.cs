using MediatR;
using WMS.Domain.Entities.InboundOrderHistoryAggregateRoot;
using WMS.Domain.Events;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Handlers;

public class QcInspectionCompletedEventHandler(
    IRepository<InboundOrderHistory> historyRepo,
    ICurrentUser currentUser) : INotificationHandler<QcInspectionCompletedEvent>
{
    public async Task Handle(QcInspectionCompletedEvent notification, CancellationToken ct)
    {
        var inspection = notification.Inspection;
        if (inspection.InboundOrderId == null)
        {
            return;
        }

        var history = new InboundOrderHistory(
            inspection.InboundOrderId.Value,
            inspection.InboundReceiptId,
            inspection.Id,
            null,
            null,
            currentUser.TenantId,
            currentUser.Email ?? "System",
            "QC",
            "QC_Completed",
            $"Completed quality inspection {inspection.InspectionNumber} at warehouse {inspection.WarehouseId}.");

        await historyRepo.AddAsync(history, ct);
    }
}
