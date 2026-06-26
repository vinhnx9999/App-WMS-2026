using WMS.Domain.Common;

namespace WMS.Domain.Entities.InboundReceiptAggregateRoot;

public class InboundReceiptItem : BaseEntity
{
    public Guid SkuId { get; private set; }
    public int ExpectedQuantity { get; private set; }
    public int ReceivedQuantity { get; private set; }
    public string? Notes { get; private set; }

    public InboundReceiptItem(Guid skuId, int expectedQuantity, int receivedQuantity, string? notes = null)
    {
        SkuId = skuId;
        ExpectedQuantity = expectedQuantity;
        ReceivedQuantity = receivedQuantity;
        Notes = notes;
    }
}
