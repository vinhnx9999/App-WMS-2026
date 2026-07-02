using WMS.Domain.Common;

namespace WMS.Domain.Entities.InboundOrderAggregateRoot;

public class InboundItem : BaseEntity
{
    public Guid InboundOrderId { get; private set; }
    public Guid SkuId { get; private set; }
    public Guid? SupplierId { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public string? SerialNumber { get; private set; }
    public string? LotNumber { get; private set; }
    public int Quantity { get; private set; }
    public int ReceivedQuantity { get; private set; }
    public string? Note { get; private set; }

    public virtual InboundOrder InboundOrder { get; private set; } = null!;

    private InboundItem() { }

    private InboundItem(
        Guid inboundOrderId,
        Guid skuId,
        int quantity,
        Guid? supplierId,
        DateOnly? expiryDate = null,
        string? serialNumber = null,
        string? lotNumber = null)
    {
        InboundOrderId = inboundOrderId;
        SkuId = skuId;
        Quantity = quantity;
        SupplierId = supplierId;
        ExpiryDate = expiryDate;
        SerialNumber = serialNumber;
        LotNumber = lotNumber;
        ReceivedQuantity = 0;
    }

    internal static InboundItem Create(
        Guid inboundOrderId,
        Guid skuId,
        int quantity,
        Guid? supplierId,
        DateOnly? expiryDate = null,
        string? serialNumber = null,
        string? lotNumber = null)
    {
        return new InboundItem(inboundOrderId, skuId, quantity, supplierId, expiryDate, serialNumber, lotNumber);
    }

    internal void AddQuantity(int quantity)
    {
        Quantity += quantity;
    }

    internal void Receive(int receivedQuantity, string? note)
    {
        ReceivedQuantity = receivedQuantity;
        Note = note;
    }
}
