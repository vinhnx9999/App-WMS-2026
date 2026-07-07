using WMS.Domain.Common;
using WMS.Domain.Entities.GoodsReceiptNoteAggregateRoot;
using WMS.Domain.Entities.InboundReceiptAggregateRoot;
using WMS.Domain.Entities.PutawayTaskAggregateRoot;
using WMS.Domain.Entities.QcInspectionAggregateRoot;

namespace WMS.Domain.Events;

public class PutawayTaskCompletedEvent(PutawayTask task) : DomainEvent
{
    public PutawayTask Task { get; } = task;
}

public class GoodsReceiptNoteGeneratedEvent(GoodsReceiptNote grn) : DomainEvent
{
    public GoodsReceiptNote Grn { get; } = grn;
}

public class InboundReceiptCompletedEvent(InboundReceipt receipt) : DomainEvent
{
    public InboundReceipt Receipt { get; } = receipt;
}

public class QcInspectionCompletedEvent(QcInspection inspection) : DomainEvent
{
    public QcInspection Inspection { get; } = inspection;
}
