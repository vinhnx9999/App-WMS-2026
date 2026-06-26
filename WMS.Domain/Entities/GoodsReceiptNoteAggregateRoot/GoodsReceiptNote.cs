using WMS.Domain.Common;
using WMS.Domain.Events;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities.GoodsReceiptNoteAggregateRoot;

public class GoodsReceiptNote : BaseEntity, IAggregateRoot
{
    public string GrnNumber { get; private set; }
    public Guid? InboundOrderId { get; private set; }
    public Guid? InboundReceiptId { get; private set; }
    public Guid PutawayTaskId { get; private set; }
    public Guid WarehouseId { get; private set; }

    private readonly List<GoodsReceiptNoteItem> _items = new();
    public IReadOnlyCollection<GoodsReceiptNoteItem> Items => _items.AsReadOnly();

    private GoodsReceiptNote(string grnNumber, Guid? inboundOrderId, Guid? inboundReceiptId, Guid putawayTaskId, Guid warehouseId)
    {
        GrnNumber = grnNumber;
        InboundOrderId = inboundOrderId;
        InboundReceiptId = inboundReceiptId;
        PutawayTaskId = putawayTaskId;
        WarehouseId = warehouseId;

        AddEvent(new GoodsReceiptNoteGeneratedEvent(this));
    }

    public static GoodsReceiptNote Create(string grnNumber, Guid? inboundOrderId, Guid? inboundReceiptId, Guid putawayTaskId, Guid warehouseId)
    {
        return new GoodsReceiptNote(grnNumber, inboundOrderId, inboundReceiptId, putawayTaskId, warehouseId);
    }

    public void AddItem(GoodsReceiptNoteItem item)
    {
        _items.Add(item);
    }
}
