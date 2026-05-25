using WMS.Application.Product.Skus.DTOs;

namespace WMS.Application.Product.Skus.Commands.ImportSkus;

public static class ImportSkuRowValidator
{
    public static ImportSkuValidationResult Validate(IReadOnlyList<ImportSkuRowInput> rows)
    {
        var normalizedRows = ImportSkuRowNormalizer.Normalize(rows);
        var errors = new List<ImportSkuRowErrorResponse>();
        var seenSkuCodes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var seenSkuUoms = new Dictionary<(string SkuCode, string UnitOfMeasureCode), int>();

        foreach (var row in normalizedRows)
        {
            if (string.IsNullOrWhiteSpace(row.SkuCode))
            {
                errors.Add(Error(row, nameof(row.SkuCode), "SKU_CODE_REQUIRED", "SKU code is required"));
                continue;
            }

            if (seenSkuCodes.TryGetValue(row.SkuCode, out var firstSkuRowNumber))
            {
                errors.Add(Error(row, nameof(row.SkuCode), "DUPLICATE_SKU_CODE", $"SKU code duplicates row {firstSkuRowNumber}"));
            }
            else
            {
                seenSkuCodes.Add(row.SkuCode, row.RowNumber);
            }

            if (row.UnitOfMeasureCode is not null)
            {
                if (row.ConversionFactor is null or <= 0)
                {
                    errors.Add(Error(row, nameof(row.ConversionFactor), "INVALID_CONVERSION_FACTOR", "Conversion factor must be greater than 0 when unit of measure is provided"));
                }

                var skuUomKey = (row.SkuCode, row.UnitOfMeasureCode);
                if (seenSkuUoms.TryGetValue(skuUomKey, out var firstSkuUomRowNumber))
                {
                    errors.Add(Error(row, nameof(row.UnitOfMeasureCode), "DUPLICATE_SKU_UOM", $"SKU and unit of measure duplicate row {firstSkuUomRowNumber}"));
                }
                else
                {
                    seenSkuUoms.Add(skuUomKey, row.RowNumber);
                }
            }
        }

        return new ImportSkuValidationResult(normalizedRows, errors);
    }

    private static ImportSkuRowErrorResponse Error(
        ImportSkuRowInput row,
        string field,
        string code,
        string message)
    {
        return new ImportSkuRowErrorResponse(row.RowNumber, row.SkuCode, field, code, message);
    }
}
