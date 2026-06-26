using WMS.Domain.Common;
using WMS.Domain.Events;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities.PutawayTaskAggregateRoot;

public enum PutawayStatus
{
    Pending = 0,
    SentToWcs = 1,
    Processing = 2,
    Completed = 3
}

public class PutawayTask : BaseEntity, IAggregateRoot
{
    public string TaskNumber { get; private set; }
    public Guid? InboundOrderId { get; private set; }
    public Guid? InboundReceiptId { get; private set; }
    public Guid? QcInspectionId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public PutawayStatus Status { get; private set; } = PutawayStatus.Pending;

    private readonly List<PutawayTaskItem> _items = new();
    public IReadOnlyCollection<PutawayTaskItem> Items => _items.AsReadOnly();

    public PutawayTask(string taskNumber, Guid? inboundOrderId, Guid? inboundReceiptId, Guid? qcInspectionId, Guid warehouseId)
    {
        TaskNumber = taskNumber;
        InboundOrderId = inboundOrderId;
        InboundReceiptId = inboundReceiptId;
        QcInspectionId = qcInspectionId;
        WarehouseId = warehouseId;
    }

    public void AddItem(PutawayTaskItem item)
    {
        _items.Add(item);
    }

    public void StartProcessing()
    {
        if (Status != PutawayStatus.Pending && Status != PutawayStatus.SentToWcs)
        {
            throw new DomainException("Task can only start processing from Pending or SentToWcs status.");
        }
        Status = PutawayStatus.Processing;
    }

    public void CompleteTask()
    {
        if (Status != PutawayStatus.Processing)
        {
            throw new DomainException("Task can only be completed from Processing status.");
        }

        foreach (var item in _items)
        {
            if (item.ActualLocationId == null && item.TargetLocationId != null)
            {
                item.CompletePutaway(item.TargetLocationId.Value);
            }

            if (item.ActualLocationId == null)
            {
                throw new DomainException($"Item for SKU {item.SkuId} does not have an actual putaway location assigned.");
            }
        }

        Status = PutawayStatus.Completed;

        // Raise PutawayTaskCompletedEvent
        AddEvent(new PutawayTaskCompletedEvent(this));
    }

    public void MarkSentToWcs()
    {
        if (Status != PutawayStatus.Pending)
        {
            throw new DomainException("Task can only be sent to WCS from Pending status.");
        }
        Status = PutawayStatus.SentToWcs;
    }
}
