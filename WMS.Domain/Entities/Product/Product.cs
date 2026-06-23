using WMS.Domain.Common;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities.Product;

public class Product : BaseEntity, IAggregateRoot
{
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


    public void Update(
        string productName,
        string? description = null,
        Guid? categoryId = null)
    {
        ProductName = productName?.Trim();
        Description = description?.Trim();
        CategoryId = categoryId;
    }

    public void Delete(string? deletedBy = null)
    {
        MarkDeleted(deletedBy);
    }

    public void Restore(string? restoredBy = null)
    {
        if (!IsDeleted)
            throw new DomainException(
                "PRODUCT_NOT_DELETED",
                "Only deleted products can be restored.");
        MarkRestored(restoredBy);
    }

}
