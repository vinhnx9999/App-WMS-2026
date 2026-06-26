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
    public ReceiptStatus Status { get; private set; } = ReceiptStatus.Draft;

    private readonly List<InboundReceiptItem> _items = new();
    public IReadOnlyCollection<InboundReceiptItem> Items => _items.AsReadOnly();

    public InboundReceipt(string receiptNumber, Guid? inboundOrderId, Guid warehouseId)
    {
        ReceiptNumber = receiptNumber;
        InboundOrderId = inboundOrderId;
        WarehouseId = warehouseId;
    }

    public void AddItem(InboundReceiptItem item)
    {
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
        AddEvent(new InboundReceiptCompletedEvent(this));
    }
}
