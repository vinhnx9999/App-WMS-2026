using MediatR;
using WMS.Domain.Entities.GoodsReceiptNoteAggregateRoot;
using WMS.Domain.Events;
using WMS.Domain.Interfaces;
using WMS.Application.Common.Service;
using WMS.Domain.Enums;

namespace WMS.Application.Inbound.Handlers;

public class GenerateGrnHandler(
    IRepository<GoodsReceiptNote> grnRepo,
    ISequenceCodeGenerator sequenceCodeGenerator) : INotificationHandler<PutawayTaskCompletedEvent>
{
    public async Task Handle(PutawayTaskCompletedEvent notification, CancellationToken ct)
    {
        var task = notification.Task;
        var grnNumber = await sequenceCodeGenerator.NextAsync(task.TenantId, CodeSequenceTypes.GoodsReceiptNote, ct);
        var grn = GoodsReceiptNote.Create(
            grnNumber,
            task.InboundOrderId,
            task.InboundReceiptId,
            task.Id,
            task.WarehouseId);

        foreach (var item in task.Items)
        {
            if (item.ActualLocationId.HasValue)
            {
                var grnItem = new GoodsReceiptNoteItem(
                    item.SkuId,
                    item.PutawayQuantity,
                    item.ActualLocationId.Value,
                    Guid.Empty); // InventoryItemId can be resolved or left empty
                grn.AddItem(grnItem);
            }
        }

        await grnRepo.AddAsync(grn, ct);
    }
}
