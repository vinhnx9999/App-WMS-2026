using Microsoft.EntityFrameworkCore;
using WMS.Domain.Entities.ProductAggregateRoot;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Interfaces;

namespace WMS.Application.Skus;

public class SkuImportRowValidationInput
{
    public Guid? Id { get; set; }
    public int RowNumber { get; set; }
    public string? ProductCode { get; set; }
    public string? SkuCode { get; set; }
    public string? Name { get; set; }
    public string? GoodsNature { get; set; }
    public string? Description { get; set; }
    public decimal? ReferencePrice { get; set; }
}

public class SkuImportValidationResult
{
    public bool IsValid { get; private set; }
    public Guid? ProductId { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static SkuImportValidationResult Valid(Guid productId)
    {
        return new SkuImportValidationResult
        {
            IsValid = true,
            ProductId = productId
        };
    }

    public static SkuImportValidationResult Invalid(string errorCode, string errorMessage)
    {
        return new SkuImportValidationResult
        {
            IsValid = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}

public class SkuImportValidationHelper(IUnitOfWork uow)
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<Dictionary<int, SkuImportValidationResult>> ValidateRowsAsync(
        Guid tenantId,
        IReadOnlyList<SkuImportRowValidationInput> rows,
        CancellationToken ct)
    {
        var results = new Dictionary<int, SkuImportValidationResult>();
        if (rows == null || rows.Count == 0)
        {
            return results;
        }

        // 1. Preload Products
        var productCodes = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.ProductCode))
            .Select(x => NormalizeCode(x.ProductCode!))
            .Distinct()
            .ToList();

        var productsByCode = productCodes.Count > 0
            ? await _uow.Repository<Product>().Query()
                .Where(x => x.TenantId == tenantId && productCodes.Contains(x.ProductCode) && !x.IsDeleted)
                .ToDictionaryAsync(x => x.ProductCode, x => x, ct)
            : new Dictionary<string, Product>();

        // 2. Preload Existing SKU Codes
        var skuCodes = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.SkuCode))
            .Select(x => NormalizeCode(x.SkuCode!))
            .Distinct()
            .ToList();

        var existingSkuCodes = skuCodes.Count > 0
            ? (await _uow.Repository<Sku>().Query()
                .Where(x => x.TenantId == tenantId && skuCodes.Contains(x.SkuCode) && !x.IsDeleted)
                .Select(x => x.SkuCode)
                .ToListAsync(ct)).ToHashSet()
            : new HashSet<string>();

        // 3. Find duplicates in import
        var duplicateSkuRowsInRequest = FindDuplicateSkuCodesInSession(rows);

        // 4. Validate each row
        foreach (var row in rows)
        {
            results[row.RowNumber] = ValidateRow(row, productsByCode, duplicateSkuRowsInRequest, existingSkuCodes);
        }

        return results;
    }

    public async Task<SkuImportValidationResult> ValidateSingleRowAsync(
        Guid tenantId,
        SkuImportRowValidationInput targetRow,
        IReadOnlyList<SkuImportRowValidationInput> allSessionRows,
        CancellationToken ct)
    {
        // 1. Check basic constraints before hitting DB
        if (targetRow.RowNumber <= 0)
        {
            return SkuImportValidationResult.Invalid("INVALID_ROW_NUMBER", "Row number must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(targetRow.ProductCode))
        {
            return SkuImportValidationResult.Invalid("PRODUCT_CODE_REQUIRED", "Product code is required.");
        }

        if (string.IsNullOrWhiteSpace(targetRow.Name))
        {
            return SkuImportValidationResult.Invalid("SKU_NAME_REQUIRED", "SKU name is required.");
        }

        if (targetRow.ReferencePrice is < 0)
        {
            return SkuImportValidationResult.Invalid("INVALID_REFERENCE_PRICE", "Reference price must be greater than or equal to zero.");
        }

        // 2. Check product existence
        var normalizedProductCode = NormalizeCode(targetRow.ProductCode);
        var product = await _uow.Repository<Product>().Query()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ProductCode == normalizedProductCode && !x.IsDeleted, ct);

        if (product == null)
        {
            return SkuImportValidationResult.Invalid("PRODUCT_NOT_FOUND", "Product code does not exist.");
        }

        // 3. Check SKU constraints
        if (!string.IsNullOrWhiteSpace(targetRow.SkuCode))
        {
            var normalizedSkuCode = NormalizeCode(targetRow.SkuCode);

            // Check session duplicates: other active rows in the session having the same normalized SKU code
            var isDuplicateInSession = allSessionRows.Any(x => 
                x.RowNumber != targetRow.RowNumber && 
                !string.IsNullOrWhiteSpace(x.SkuCode) && 
                NormalizeCode(x.SkuCode) == normalizedSkuCode);

            if (isDuplicateInSession)
            {
                return SkuImportValidationResult.Invalid("DUPLICATE_SKU_IN_IMPORT", "SKU code is duplicated in the import rows.");
            }

            // Check database duplicate
            var skuExists = await _uow.Repository<Sku>().Query()
                .AnyAsync(x => x.TenantId == tenantId && x.SkuCode == normalizedSkuCode && !x.IsDeleted, ct);

            if (skuExists)
            {
                return SkuImportValidationResult.Invalid("DUPLICATE_SKU", "SKU code already exists for this tenant.");
            }
        }

        return SkuImportValidationResult.Valid(product.Id);
    }

    private static SkuImportValidationResult ValidateRow(
        SkuImportRowValidationInput row,
        IReadOnlyDictionary<string, Product> productsByCode,
        IReadOnlySet<int> duplicateSkuRowsInRequest,
        IReadOnlySet<string> existingSkuCodes)
    {
        if (row.RowNumber <= 0)
        {
            return SkuImportValidationResult.Invalid("INVALID_ROW_NUMBER", "Row number must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(row.ProductCode))
        {
            return SkuImportValidationResult.Invalid("PRODUCT_CODE_REQUIRED", "Product code is required.");
        }

        var normalizedProductCode = NormalizeCode(row.ProductCode);
        if (!productsByCode.TryGetValue(normalizedProductCode, out var product))
        {
            return SkuImportValidationResult.Invalid("PRODUCT_NOT_FOUND", "Product code does not exist.");
        }

        if (string.IsNullOrWhiteSpace(row.Name))
        {
            return SkuImportValidationResult.Invalid("SKU_NAME_REQUIRED", "SKU name is required.");
        }

        if (row.ReferencePrice is < 0)
        {
            return SkuImportValidationResult.Invalid("INVALID_REFERENCE_PRICE", "Reference price must be greater than or equal to zero.");
        }

        if (!string.IsNullOrWhiteSpace(row.SkuCode))
        {
            var normalizedSkuCode = NormalizeCode(row.SkuCode);

            if (duplicateSkuRowsInRequest.Contains(row.RowNumber))
            {
                return SkuImportValidationResult.Invalid("DUPLICATE_SKU_IN_IMPORT", "SKU code is duplicated in the import rows.");
            }

            if (existingSkuCodes.Contains(normalizedSkuCode))
            {
                return SkuImportValidationResult.Invalid("DUPLICATE_SKU", "SKU code already exists for this tenant.");
            }
        }

        return SkuImportValidationResult.Valid(product.Id);
    }

    private static HashSet<int> FindDuplicateSkuCodesInSession(
        IReadOnlyList<SkuImportRowValidationInput> rows)
    {
        var firstRowBySkuCode = new Dictionary<string, int>();
        var duplicateRows = new HashSet<int>();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.SkuCode))
            {
                continue;
            }

            var normalizedSkuCode = NormalizeCode(row.SkuCode);
            if (firstRowBySkuCode.ContainsKey(normalizedSkuCode))
            {
                duplicateRows.Add(row.RowNumber);
            }
            else
            {
                firstRowBySkuCode[normalizedSkuCode] = row.RowNumber;
            }
        }

        return duplicateRows;
    }

    private static string NormalizeCode(string code)
    {
        return code.Trim().ToUpperInvariant();
    }
}
