using WMS.Domain.Common;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities.InboundOrderHistoryAggregateRoot;

public class InboundOrderHistory : BaseEntity, IAggregateRoot
{
    public Guid InboundOrderId { get; private set; }
    public Guid? InboundReceiptId { get; private set; }
    public Guid? QcInspectionId { get; private set; }
    public Guid? PutawayTaskId { get; private set; }
    public Guid? GoodsReceiptNoteId { get; private set; }
    public string PerformedBy { get; private set; }
    public string Step { get; private set; }
    public string Action { get; private set; }
    public string Details { get; private set; }
    public DateTime Timestamp { get; private set; }

    private InboundOrderHistory()
    {
        PerformedBy = null!;
        Step = null!;
        Action = null!;
        Details = null!;
    }

    public InboundOrderHistory(
        Guid inboundOrderId,
        Guid? inboundReceiptId,
        Guid? qcInspectionId,
        Guid? putawayTaskId,
        Guid? goodsReceiptNoteId,
        Guid tenantId,
        string performedBy,
        string step,
        string action,
        string details)
    {
        InboundOrderId = inboundOrderId;
        InboundReceiptId = inboundReceiptId;
        QcInspectionId = qcInspectionId;
        PutawayTaskId = putawayTaskId;
        GoodsReceiptNoteId = goodsReceiptNoteId;
        TenantId = tenantId;
        PerformedBy = performedBy;
        Step = step;
        Action = action;
        Details = details;
        Timestamp = DateTime.UtcNow;
    }
}
