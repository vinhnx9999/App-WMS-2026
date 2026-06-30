using WMS.Domain.Common;
using WMS.Domain.Enums;
using WMS.Domain.Events;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities.PutawayTaskAggregateRoot;

public class PutawayTask : BaseEntity, IAggregateRoot
{
    public string PutawayTaskNumber { get; private set; } = default!;
    public Guid? InboundOrderId { get; private set; }
    public Guid? InboundReceiptId { get; private set; }
    public Guid? QcInspectionId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public PutawayStatus Status { get; private set; } = PutawayStatus.Pending;

    private readonly List<PutawayTaskItem> _items = new();
    public IReadOnlyCollection<PutawayTaskItem> Items => _items.AsReadOnly();

    private PutawayTask() { }

    private PutawayTask(Guid tenantId, string putawayTaskNumber, Guid? inboundOrderId, Guid? inboundReceiptId, Guid? qcInspectionId, Guid warehouseId)
    {
        TenantId = tenantId;
        PutawayTaskNumber = putawayTaskNumber;
        InboundOrderId = inboundOrderId;
        InboundReceiptId = inboundReceiptId;
        QcInspectionId = qcInspectionId;
        WarehouseId = warehouseId;
    }

    public static PutawayTask Create(Guid tenantId, string putawayTaskNumber, Guid? inboundOrderId, Guid? inboundReceiptId, Guid? qcInspectionId, Guid warehouseId)
    {
        return new PutawayTask(tenantId, putawayTaskNumber, inboundOrderId, inboundReceiptId, qcInspectionId, warehouseId);
    }

    public void AddItem(
        Guid skuId,
        int putawayQuantity,
        Guid? targetLocationId = null,
        Guid? actualLocationId = null,
        Guid? palletId = null,
        Guid? supplierId = null,
        DateTime? expiryDate = null,
        string? serialNumber = null,
        string? lotNumber = null)
    {
        var item = PutawayTaskItem.Create(
            TenantId,
            skuId,
            putawayQuantity,
            targetLocationId,
            actualLocationId,
            palletId,
            supplierId,
            expiryDate,
            serialNumber,
            lotNumber);

        _items.Add(item);
    }

    public void StartProcessing()
    {
        if (Status != PutawayStatus.Pending)
        {
            throw new DomainException("Task can only start processing from Pending or SentToWcs status.");
        }
        Status = PutawayStatus.Processing;
    }

    public void CompleteTask()
    {
        // if (Status != PutawayStatus.Processing)
        // {
        //     throw new DomainException("Task can only be completed from Processing status.");
        // }

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

    public void RequestWcsMovements(IReadOnlyList<WcsMovementItem> items)
    {
        if (items.Any())
        {
            AddEvent(new WcsTaskRequiredEvent(TenantId, WarehouseId, Id, items));
        }
    }
}
