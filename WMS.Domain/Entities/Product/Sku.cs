using WMS.Domain.Common;
using WMS.Domain.Extensions;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities.Product;

public class Sku : BaseEntity, IAggregateRoot
{
    private readonly List<SkuAttributeValue> _attributes = new();
    private readonly List<SkuUnitOfMeasure> _allowedUnits = new();
    private Sku() { }
    private Sku(
      Guid tenantId,
      Guid productId,
      string skuCode,
      string? name,
      string? goodsNature,
      string? description,
      decimal? referencePrice,
      string? barcode = null,
      int minQuantity = 0)
    {
        TenantId = tenantId;
        ProductId = productId;
        SkuCode = skuCode;
        Name = name;
        GoodsNature = goodsNature;
        Description = description;
        ReferencePrice = referencePrice;
        Barcode = barcode;
        MinQuantity = minQuantity;
    }
    /// <summary>
    /// Product ID
    /// </summary>
    public Guid? ProductId { get; private set; }

    /// <summary>
    /// Sku Code
    /// </summary>
    public string SkuCode { get; private set; } = null!;

    /// <summary>
    /// Display Name
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>
    /// Goods nature
    /// </summary>
    public string? GoodsNature { get; private set; }
    /// <summary>
    /// Description of the SKU
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Reference price for the SKU (optional, can be used for valuation or other purposes)
    /// </summary>
    public decimal? ReferencePrice { get; private set; }

    /// <summary>
    /// Barcode of the SKU
    /// </summary>
    public string? Barcode { get; private set; }

    /// <summary>
    /// Minimum safety stock quantity for low stock alerting
    /// </summary>
    public int MinQuantity { get; private set; }

    /// <summary>
    /// Attribute values associated with this SKU
    /// </summary>
    public IReadOnlyCollection<SkuAttributeValue> Attributes => _attributes.AsReadOnly();

    /// <summary>
    /// UOMs allowed for this SKU, along with their conversion factors
    /// </summary>
    public IReadOnlyCollection<SkuUnitOfMeasure> AllowedUnits => _allowedUnits.AsReadOnly();


    #region Domain method

    public static Sku Create(
     Guid tenantId,
     Guid productId,
     string skuCode,
     string? name,
     string? goodsNature,
     string? description,
     decimal? referencePrice,
     string? barcode = null,
     int minQuantity = 0)
    {
        if (tenantId == Guid.Empty)
            throw new DomainException("TenantId is required.");

        if (productId == Guid.Empty)
            throw new DomainException("ProductId is required.");

        if (string.IsNullOrWhiteSpace(skuCode))
            throw new DomainException("SKU code is required.");

        if (referencePrice < 0)
            throw new DomainException("Reference price cannot be negative.");

        if (minQuantity < 0)
            throw new DomainException("Min quantity cannot be negative.");

        return new Sku(
            tenantId,
            productId,
            Utilities.NormalizeCode(skuCode),
            Utilities.NormalizeNullable(name),
            Utilities.NormalizeNullable(goodsNature),
            Utilities.NormalizeNullable(description),
            referencePrice,
            Utilities.NormalizeNullable(barcode),
            minQuantity);
    }




    /// <summary>
    /// Updates scalar fields of this SKU.
    /// </summary>
    public void Update(
        string? name = null,
        string? goodsNature = null,
        string? description = null,
        decimal? referencePrice = null,
        string? barcode = null,
        int? minQuantity = null)
    {
        if (referencePrice is < 0)
        {
            throw new DomainException(
                "INVALID_REFERENCE_PRICE",
                "Reference price must be greater than or equal to zero.");
        }

        if (minQuantity is < 0)
        {
            throw new DomainException(
                "INVALID_MIN_QUANTITY",
                "Minimum quantity must be greater than or equal to zero.");
        }

        Name = name?.Trim();
        GoodsNature = goodsNature?.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        ReferencePrice = referencePrice;
        Barcode = barcode?.Trim();
        if (minQuantity.HasValue)
        {
            MinQuantity = minQuantity.Value;
        }
    }

    public void Delete(string? deletedBy = null)
    {
        MarkDeleted(deletedBy);
    }

    protected void Restore(string? updatedBy)
    {
        MarkRestored();
    }

    /// <summary>
    /// Adding new Uom for Sku
    /// </summary>
    /// <param name="unitOfMeasureId"></param>
    /// <param name="conversionFactor"></param>
    /// <param name="updatedBy"></param>
    /// <exception cref="DomainException"></exception>
    protected void AllowUnitOfMeasure(Guid unitOfMeasureId, string? updatedBy)
    {
        if (unitOfMeasureId == Guid.Empty)
        {
            throw new DomainException(
                "INVALID_UNIT_OF_MEASURE",
                "Unit of measure is required.");
        }

        if (_allowedUnits.Any(x =>
            !x.IsDeleted &&
            x.UnitOfMeasureId == unitOfMeasureId))
        {
            return;
        }

        _allowedUnits.Add(new SkuUnitOfMeasure(
            tenantId: TenantId,
            skuId: Id,
            unitOfMeasureId: unitOfMeasureId));
    }

    protected void RemoveUnitOfMeasure(Guid unitOfMeasureId, string? deletedBy)
    {
        var allowedUnit = _allowedUnits.FirstOrDefault(x => !x.IsDeleted &&
                                         x.UnitOfMeasureId == unitOfMeasureId);

        if (allowedUnit is null)
        {
            return;
        }

        allowedUnit.Delete(deletedBy);
    }

    /// <summary>
    /// Adds an attribute value to this SKU. Idempotent per attributeId.
    /// </summary>
    public void AddAttribute(Guid attributeId, string value)
    {
        if (attributeId == Guid.Empty)
        {
            throw new DomainException(
                "INVALID_ATTRIBUTE",
                "Attribute is required.");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                "INVALID_ATTRIBUTE_VALUE",
                "Attribute value is required.");
        }

        if (_attributes.Any(x => !x.IsDeleted && x.AttributeId == attributeId))
        {
            return;
        }

        _attributes.Add(new SkuAttributeValue(
            tenantId: TenantId,
            skuId: Id,
            attributeId: attributeId,
            value: value));
    }

    #endregion Domain method

}
