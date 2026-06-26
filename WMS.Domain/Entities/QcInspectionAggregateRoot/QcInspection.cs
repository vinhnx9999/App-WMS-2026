using WMS.Domain.Common;
using WMS.Domain.Events;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities.QcInspectionAggregateRoot;

public enum QcStatus
{
    Pending = 0,
    Inspecting = 1,
    Completed = 2
}

public class QcInspection : BaseEntity, IAggregateRoot
{
    public string InspectionNumber { get; private set; }
    public Guid? InboundOrderId { get; private set; }
    public Guid? InboundReceiptId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public QcStatus Status { get; private set; } = QcStatus.Pending;

    private readonly List<QcInspectionItem> _items = new();
    public IReadOnlyCollection<QcInspectionItem> Items => _items.AsReadOnly();

    public QcInspection(string inspectionNumber, Guid? inboundOrderId, Guid? inboundReceiptId, Guid warehouseId)
    {
        InspectionNumber = inspectionNumber;
        InboundOrderId = inboundOrderId;
        InboundReceiptId = inboundReceiptId;
        WarehouseId = warehouseId;
    }

    public void AddItem(QcInspectionItem item)
    {
        _items.Add(item);
    }

    public void StartInspection()
    {
        if (Status != QcStatus.Pending)
        {
            throw new DomainException("Inspection can only be started from Pending status.");
        }
        Status = QcStatus.Inspecting;
    }

    public void CompleteInspection()
    {
        if (Status != QcStatus.Inspecting)
        {
            throw new DomainException("Inspection can only be completed from Inspecting status.");
        }
        Status = QcStatus.Completed;
        AddEvent(new QcInspectionCompletedEvent(this));
    }
}
