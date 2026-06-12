using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.Product;
using WMS.Domain.Interfaces;

namespace WMS.Application.Skus.Commands.ImportSku;

public sealed class CreateSkuImportSessionCommandHandler(
    IUnitOfWork uow)
    : IRequestHandler<CreateSkuImportSessionCommand, CreateSkuImportSessionResponse>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<CreateSkuImportSessionResponse> Handle(
        CreateSkuImportSessionCommand request,
        CancellationToken ct)
    {
        var rows = request.Rows ?? [];

        var session = SkuImportSession.Create(
            tenantId: request.TenantId,
            sourceFileName: request.SourceFileName);

        if (rows.Count == 0)
        {
            session.AddRow(
                rowNumber: 1,
                productCode: null,
                productId: null,
                skuCode: null,
                name: null,
                goodsNature: null,
                description: null,
                referencePrice: null,
                isValid: false,
                errorCode: "EMPTY_IMPORT",
                errorMessage: "Import rows are required.");

            await _uow.Repository<SkuImportSession>().AddAsync(session, ct);
            await _uow.SaveChangesAsync(ct);

            return MapResponse(session);
        }

        ValidateDuplicateRowNumbers(rows);

        var productsByCode = await LoadProductsByCode(
            request.TenantId,
            rows,
            ct);

        var duplicateSkuRowsInRequest = FindDuplicateManualSkuCodesInRequest(rows);

        var existingSkuCodes = await LoadExistingManualSkuCodes(
            request.TenantId,
            rows,
            ct);

        foreach (var row in rows)
        {
            var validation = ValidateRow(
                row,
                productsByCode,
                duplicateSkuRowsInRequest,
                existingSkuCodes);

            session.AddRow(
                rowNumber: row.RowNumber,
                productCode: row.ProductCode,
                productId: validation.ProductId,
                skuCode: row.SkuCode,
                name: row.Name,
                goodsNature: row.GoodsNature,
                description: row.Description,
                referencePrice: row.ReferencePrice,
                isValid: validation.IsValid,
                errorCode: validation.ErrorCode,
                errorMessage: validation.ErrorMessage);
        }

        await _uow.Repository<SkuImportSession>().AddAsync(session, ct);
        await _uow.SaveChangesAsync(ct);

        return MapResponse(session);
    }

    private static void ValidateDuplicateRowNumbers(
        IReadOnlyList<ImportSkuRowRequest> rows)
    {
        var duplicateRowNumber = rows
            .GroupBy(x => x.RowNumber)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicateRowNumber is not null)
        {
            throw new AppException(
                400,
                "DUPLICATE_ROW_NUMBER",
                $"Row number {duplicateRowNumber.Key} is duplicated.");
        }
    }

    private async Task<Dictionary<string, Domain.Entities.Product.Product>> LoadProductsByCode(
        Guid tenantId,
        IReadOnlyList<ImportSkuRowRequest> rows,
        CancellationToken ct)
    {
        var productCodes = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.ProductCode))
            .Select(x => NormalizeCode(x.ProductCode!))
            .Distinct()
            .ToList();

        if (productCodes.Count == 0)
        {
            return [];
        }

        return await _uow.Repository<Domain.Entities.Product.Product>().Query()
            .Where(x =>
                x.TenantId == tenantId
                && productCodes.Contains(x.ProductCode)
                && !x.IsDeleted)
            .ToDictionaryAsync(x => x.ProductCode, x => x, ct);
    }

    private static HashSet<int> FindDuplicateManualSkuCodesInRequest(
        IReadOnlyList<ImportSkuRowRequest> rows)
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

    private async Task<HashSet<string>> LoadExistingManualSkuCodes(
        Guid tenantId,
        IReadOnlyList<ImportSkuRowRequest> rows,
        CancellationToken ct)
    {
        var skuCodes = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.SkuCode))
            .Select(x => NormalizeCode(x.SkuCode!))
            .Distinct()
            .ToList();

        if (skuCodes.Count == 0)
        {
            return [];
        }

        var existingSkuCodes = await _uow.Repository<Sku>().Query()
            .Where(x =>
                x.TenantId == tenantId
                && skuCodes.Contains(x.SkuCode)
                && !x.IsDeleted)
            .Select(x => x.SkuCode)
            .ToListAsync(ct);

        return existingSkuCodes.ToHashSet();
    }

    private static RowValidationResult ValidateRow(
        ImportSkuRowRequest row,
        IReadOnlyDictionary<string, Domain.Entities.Product.Product> productsByCode,
        IReadOnlySet<int> duplicateSkuRowsInRequest,
        IReadOnlySet<string> existingSkuCodes)
    {
        if (row.RowNumber <= 0)
        {
            return RowValidationResult.Invalid(
                "INVALID_ROW_NUMBER",
                "Row number must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(row.ProductCode))
        {
            return RowValidationResult.Invalid(
                "PRODUCT_CODE_REQUIRED",
                "Product code is required.");
        }

        var normalizedProductCode = NormalizeCode(row.ProductCode);

        if (!productsByCode.TryGetValue(normalizedProductCode, out var product))
        {
            return RowValidationResult.Invalid(
                "PRODUCT_NOT_FOUND",
                "Product code does not exist.");
        }

        if (string.IsNullOrWhiteSpace(row.Name))
        {
            return RowValidationResult.Invalid(
                "SKU_NAME_REQUIRED",
                "SKU name is required.");
        }

        if (row.ReferencePrice is < 0)
        {
            return RowValidationResult.Invalid(
                "INVALID_REFERENCE_PRICE",
                "Reference price must be greater than or equal to zero.");
        }

        if (!string.IsNullOrWhiteSpace(row.SkuCode))
        {
            var normalizedSkuCode = NormalizeCode(row.SkuCode);

            if (duplicateSkuRowsInRequest.Contains(row.RowNumber))
            {
                return RowValidationResult.Invalid(
                    "DUPLICATE_SKU_IN_IMPORT",
                    "SKU code is duplicated in the import rows.");
            }

            if (existingSkuCodes.Contains(normalizedSkuCode))
            {
                return RowValidationResult.Invalid(
                    "DUPLICATE_SKU",
                    "SKU code already exists for this tenant.");
            }
        }

        return RowValidationResult.Valid(product.Id);
    }

    private static CreateSkuImportSessionResponse MapResponse(
        SkuImportSession session)
    {
        return new CreateSkuImportSessionResponse(
            ImportSessionId: session.Id,
            Status: session.Status,
            TotalRows: session.TotalRows,
            ValidRows: session.ValidRows,
            InvalidRows: session.InvalidRows,
            Rows: session.Rows
                .OrderBy(x => x.RowNumber)
                .Select(x => new SkuImportRowPreview(
                    ImportRowId: x.Id,
                    RowNumber: x.RowNumber,
                    ProductCode: x.ProductCode,
                    ProductId: x.ProductId,
                    SkuCode: x.SkuCode,
                    Name: x.Name,
                    GoodsNature: x.GoodsNature,
                    Description: x.Description,
                    ReferencePrice: x.ReferencePrice,
                    IsValid: x.IsValid,
                    ErrorCode: x.ErrorCode,
                    ErrorMessage: x.ErrorMessage))
                .ToList());
    }

    private static string NormalizeCode(string code)
    {
        return code.Trim().ToUpperInvariant();
    }

    private sealed record RowValidationResult(
        bool IsValid,
        Guid? ProductId,
        string? ErrorCode,
        string? ErrorMessage)
    {
        public static RowValidationResult Valid(Guid productId)
        {
            return new RowValidationResult(
                IsValid: true,
                ProductId: productId,
                ErrorCode: null,
                ErrorMessage: null);
        }

        public static RowValidationResult Invalid(
            string errorCode,
            string errorMessage)
        {
            return new RowValidationResult(
                IsValid: false,
                ProductId: null,
                ErrorCode: errorCode,
                ErrorMessage: errorMessage);
        }
    }
}