namespace WMS.Application.Product.Skus.DTOs;

public sealed record CreateSkuRequest(
    Guid ProductId,
    string SkuCode,
    string? Name = null,
    string? GoodsNature = null,
    string? Description = null,
    decimal? Price = null);
