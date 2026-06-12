using WMS.Domain.Common;

namespace WMS.Domain.Entities.Product;

public class SkuUnitOfMeasure : BaseEntity
{
    private SkuUnitOfMeasure() { }

    /// <summary>
    /// Sku ID
    /// </summary>
    public Guid SkuId { get; private set; }
    public Guid UnitOfMeasureId { get; set; }


    internal SkuUnitOfMeasure(
        Guid tenantId,
        Guid skuId,
        Guid unitOfMeasureId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException(
                "INVALID_TENANT",
                "Tenant is required.");
        }

        if (skuId == Guid.Empty)
        {
            throw new DomainException(
                "INVALID_SKU",
                "SKU is required.");
        }

        if (unitOfMeasureId == Guid.Empty)
        {
            throw new DomainException(
                "INVALID_UNIT_OF_MEASURE",
                "Unit of measure is required.");
        }

        TenantId = tenantId;
        SkuId = skuId;
        UnitOfMeasureId = unitOfMeasureId;
    }

    internal void Delete(string? deletedBy)
    {
        MarkDeleted(deletedBy);
    }

    internal void Restore(string? restoredBy)
    {
        MarkRestored(restoredBy);
    }
}
