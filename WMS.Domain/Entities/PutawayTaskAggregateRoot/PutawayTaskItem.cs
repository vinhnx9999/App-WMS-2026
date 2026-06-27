using WMS.Domain.Common;

namespace WMS.Domain.Entities.PutawayTaskAggregateRoot;

public class PutawayTaskItem : BaseEntity
{
    public Guid SkuId { get; private set; }
    public int PutawayQuantity { get; private set; }
    public Guid? TargetLocationId { get; private set; }
    public Guid? ActualLocationId { get; private set; }
    public Guid? PalletId { get; private set; }
    public Guid? SupplierId { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public string? SerialNumber { get; private set; }
    public string? LotNumber { get; private set; }

    private PutawayTaskItem() { }

    private PutawayTaskItem(
        Guid tenantId,
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
        TenantId = tenantId;
        SkuId = skuId;
        PutawayQuantity = putawayQuantity;
        TargetLocationId = targetLocationId;
        ActualLocationId = actualLocationId;
        PalletId = palletId;
        SupplierId = supplierId;
        ExpiryDate = expiryDate;
        SerialNumber = serialNumber;
        LotNumber = lotNumber;
    }

    internal static PutawayTaskItem Create(
        Guid tenantId,
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
        return new PutawayTaskItem(
            tenantId,
            skuId,
            putawayQuantity,
            targetLocationId,
            actualLocationId,
            palletId,
            supplierId,
            expiryDate,
            serialNumber,
            lotNumber);
    }

    public void CompletePutaway(
        Guid actualLocationId,
        Guid? palletId = null,
        Guid? supplierId = null,
        DateTime? expiryDate = null,
        string? serialNumber = null,
        string? lotNumber = null)
    {
        ActualLocationId = actualLocationId;
        PalletId = palletId;
        SupplierId = supplierId;
        ExpiryDate = expiryDate;
        SerialNumber = serialNumber;
        LotNumber = lotNumber;
    }
}
