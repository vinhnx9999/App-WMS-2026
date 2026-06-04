using WMS.Application.Product.Skus.DTOs;

namespace WMS.Application.Product.Skus.Commands.ImportSkus;

public static class ImportSkuRowNormalizer
{
    public static ImportSkuRowInput Normalize(ImportSkuRowInput row)
    {
        return row with
        {
            ProductCode = NormalizeRequired(row.ProductCode),
            SkuCode = NormalizeRequired(row.SkuCode),
            SkuName = NormalizeOptional(row.SkuName),
            CategoryName = NormalizeOptional(row.CategoryName),
            GoodsNature = NormalizeOptional(row.GoodsNature),
            SpecificationCode = NormalizeOptional(row.SpecificationCode),
            UnitOfMeasureCode = NormalizeOptional(row.UnitOfMeasureCode)
        };
    }

    public static IReadOnlyList<ImportSkuRowInput> Normalize(IEnumerable<ImportSkuRowInput> rows)
    {
        return rows.Select(Normalize).ToList();
    }

    private static string? NormalizeRequired(string? value)
    {
        return value?.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
