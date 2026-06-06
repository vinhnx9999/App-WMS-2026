using WMS.Domain.Common;
using WMS.Domain.Extensions;

namespace WMS.Domain.Entities.Product;

public class SkuImportRow : BaseEntity
{
    private SkuImportRow()
    {
    }

    private SkuImportRow(
        Guid tenantId,
        Guid importSessionId,
        int rowNumber,
        string? productCode,
        Guid? productId,
        string? skuCode,
        string? name,
        string? goodsNature,
        string? description,
        decimal? referencePrice,
        bool isValid,
        string? errorCode,
        string? errorMessage)
    {
        TenantId = tenantId;
        ImportSessionId = importSessionId;
        RowNumber = rowNumber;
        ProductCode = NormalizeCodeOrNull(productCode);
        ProductId = productId;
        SkuCode = NormalizeCodeOrNull(skuCode);
        Name = Utilities.NormalizeNullable(name);
        GoodsNature = Utilities.NormalizeNullable(goodsNature);
        Description = Utilities.NormalizeNullable(description);
        ReferencePrice = referencePrice;
        IsValid = isValid;
        ErrorCode = Utilities.NormalizeNullable(errorCode);
        ErrorMessage = Utilities.NormalizeNullable(errorMessage);
    }

    public Guid ImportSessionId { get; private set; }

    public int RowNumber { get; private set; }

    public string? ProductCode { get; private set; }

    public Guid? ProductId { get; private set; }

    public string? SkuCode { get; private set; }

    public string? Name { get; private set; }

    public string? GoodsNature { get; private set; }

    public string? Description { get; private set; }

    public decimal? ReferencePrice { get; private set; }

    public bool IsValid { get; private set; }

    public string? ErrorCode { get; private set; }

    public string? ErrorMessage { get; private set; }

    public Guid? CreatedSkuId { get; private set; }

    public static SkuImportRow Create(
        Guid tenantId,
        Guid importSessionId,
        int rowNumber,
        string? productCode,
        Guid? productId,
        string? skuCode,
        string? name,
        string? goodsNature,
        string? description,
        decimal? referencePrice,
        bool isValid,
        string? errorCode,
        string? errorMessage)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("TenantId is required.");
        }

        if (importSessionId == Guid.Empty)
        {
            throw new DomainException("ImportSessionId is required.");
        }

        if (rowNumber <= 0)
        {
            throw new DomainException(
                "INVALID_ROW_NUMBER",
                "Row number must be greater than zero.");
        }

        if (referencePrice is < 0)
        {
            throw new DomainException(
                "INVALID_REFERENCE_PRICE",
                "Reference price must be greater than or equal to zero.");
        }

        if (!isValid && string.IsNullOrWhiteSpace(errorCode))
        {
            throw new DomainException(
                "IMPORT_ROW_ERROR_REQUIRED",
                "Invalid import row must have an error code.");
        }

        return new SkuImportRow(
            tenantId,
            importSessionId,
            rowNumber,
            productCode,
            productId,
            skuCode,
            name,
            goodsNature,
            description,
            referencePrice,
            isValid,
            errorCode,
            errorMessage);
    }

    public void MarkInvalid(
        string errorCode,
        string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            throw new DomainException(
                "IMPORT_ROW_ERROR_REQUIRED",
                "Error code is required.");
        }

        IsValid = false;
        ErrorCode = errorCode.Trim().ToUpperInvariant();
        ErrorMessage = Utilities.NormalizeNullable(errorMessage);
    }

    public void MarkValid(Guid productId)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException(
                "INVALID_PRODUCT",
                "Product id is required.");
        }

        IsValid = true;
        ProductId = productId;
        ErrorCode = null;
        ErrorMessage = null;
    }

    public void AttachCreatedSku(Guid skuId)
    {
        if (skuId == Guid.Empty)
        {
            throw new DomainException(
                "INVALID_SKU",
                "SKU id is required.");
        }

        if (!IsValid)
        {
            throw new DomainException(
                "INVALID_IMPORT_ROW",
                "Cannot attach SKU to an invalid import row.");
        }

        CreatedSkuId = skuId;
    }

    private static string? NormalizeCodeOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();
    }
}