using WMS.Domain.Common;

namespace WMS.Domain.Entities.Product;

public class Product : BaseEntity
{
    private readonly List<Sku> _skus = new();

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private Product() { }

    /// <summary>
    /// Product code
    /// </summary>
    public string ProductCode { get; private set; } = default!;

    /// <summary>
    /// Product name
    /// </summary>
    public string? ProductName { get; private set; }

    /// <summary>
    /// Description of the product
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Category ID
    /// </summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>
    /// Sku Navigation property
    /// </summary>
    public IReadOnlyCollection<Sku> Skus => _skus.AsReadOnly();

    public static Product Create(
            Guid tenantId,
            string productCode,
            string? productName = null,
            string? description = null,
            Guid? categoryId = null)
    {
        return new Product
        {
            TenantId = tenantId,
            ProductCode = productCode,
            ProductName = productName?.Trim(),
            Description = description?.Trim(),
            CategoryId = categoryId
        };
    }


    internal void Delete(string? deletedBy = null)
    {
        if (_skus.Any(x => !x.IsDeleted))
        {
            throw new DomainException(
                "PRODUCT_HAS_ACTIVE_SKUS",
                "Product cannot be deleted while it has active SKUs.");
        }
        MarkDeleted(deletedBy);
    }

    internal void Restore(string? restoredBy = null)
    {
        if (!IsDeleted)
            throw new DomainException(
                "PRODUCT_NOT_DELETED",
                "Only deleted products can be restored.");
        MarkRestored(restoredBy);
    }

    public void AllowSkuUnitOfMeasure(
    Guid skuId,
    Guid unitOfMeasureId,
    string? updatedBy)
    {
        var sku = _skus.FirstOrDefault(x =>
                        x.Id == skuId &&
                        !x.IsDeleted);

        if (sku is null)
        {
            throw new DomainException(
            "SKU_NOT_FOUND",
            $"Active SKU with ID {skuId} was not found for this product.");
        }

        sku.AllowUnitOfMeasure(unitOfMeasureId, updatedBy);

    }

}
