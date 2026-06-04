namespace WMS.Application.Product.Skus.DTOs;

public sealed record ImportSkuRowInput(
    int RowNumber,
    string? ProductCode,
    string? SkuCode,
    string? SkuName,
    string? CategoryName,
    string? GoodsNature,
    string? SpecificationCode,
    string? UnitOfMeasureCode,
    decimal? ConversionFactor);
