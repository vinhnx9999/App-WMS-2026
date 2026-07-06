using WMS.Domain.Common;
using WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Events;

namespace WMS.Domain.Entities.InboundReceiptAggregateRoot;

public class InboundReceipt : BaseEntity
{
    public string ReceiptNumber { get; private set; }
    public Guid? InboundOrderId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public ReceiptStatus Status { get; private set; } = ReceiptStatus.Receiving;

    private readonly List<InboundReceiptItem> _items = new();
    public IReadOnlyCollection<InboundReceiptItem> Items => _items.AsReadOnly();

    private InboundReceipt() { }

    public InboundReceipt(Guid tenantId, string receiptNumber, Guid? inboundOrderId, Guid warehouseId)
    {
        TenantId = tenantId;
        ReceiptNumber = receiptNumber;
        InboundOrderId = inboundOrderId;
        WarehouseId = warehouseId;
    }

    internal void AddItem(InboundReceiptItem item)
    {
        _items.Add(item);
    }

    public void AddItem(
        Guid skuId,
        int expectedQuantity,
        int receivedQuantity,
        string? notes = null,
        Guid? supplierId = null,
        DateOnly? expiryDate = null,
        string? serialNumber = null,
        string? lotNumber = null)
    {
        var item = new InboundReceiptItem(
            TenantId,
            skuId,
            expectedQuantity,
            receivedQuantity,
            notes,
            supplierId,
            expiryDate,
            serialNumber,
            lotNumber);
        _items.Add(item);
    }

    public void CompleteReceipt(InboundWorkflowConfig config, int totalReceivedSoFarAcrossAllReceipts, int expectedPoQty)
    {
        if (Status == ReceiptStatus.Completed)
        {
            return;
        }

        if (totalReceivedSoFarAcrossAllReceipts > expectedPoQty)
        {
            if (!config.AllowOverReceive)
            {
                throw new DomainException("Over-receiving is not allowed for this inbound workflow.");
            }

            if (config.OverReceiveTolerancePercentage.HasValue)
            {
                var allowedLimit = expectedPoQty * (1 + config.OverReceiveTolerancePercentage.Value / 100m);
                if (totalReceivedSoFarAcrossAllReceipts > allowedLimit)
                {
                    throw new DomainException($"Received quantity exceeds the allowed tolerance limit of {config.OverReceiveTolerancePercentage.Value}%.");
                }
            }
        }

        Status = ReceiptStatus.Completed;
        AddEvent(new CreateInboundReceiptEvent(this));
    }
}
