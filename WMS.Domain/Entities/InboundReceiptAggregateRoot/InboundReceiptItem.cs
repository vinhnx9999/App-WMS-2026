using WMS.Domain.Common;

namespace WMS.Domain.Entities.InboundReceiptAggregateRoot;

public class InboundReceiptItem : BaseEntity
{
    public Guid SkuId { get; private set; }
    public int ExpectedQuantity { get; private set; }
    public int ReceivedQuantity { get; private set; }
    public string? Notes { get; private set; }
    public Guid? SupplierId { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public string? SerialNumber { get; private set; }
    public string? LotNumber { get; private set; }

    private InboundReceiptItem() { }

    internal InboundReceiptItem(
        Guid tenantId,
        Guid skuId,
        int expectedQuantity,
        int receivedQuantity,
        string? notes = null,
        Guid? supplierId = null,
        DateOnly? expiryDate = null,
        string? serialNumber = null,
        string? lotNumber = null)
    {
        TenantId = tenantId;
        SkuId = skuId;
        ExpectedQuantity = expectedQuantity;
        ReceivedQuantity = receivedQuantity;
        Notes = notes;
        SupplierId = supplierId;
        ExpiryDate = expiryDate;
        SerialNumber = serialNumber;
        LotNumber = lotNumber;
    }
}
