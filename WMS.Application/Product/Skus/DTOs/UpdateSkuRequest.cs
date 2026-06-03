namespace WMS.Application.Product.Skus.DTOs;

public sealed record UpdateSkuRequest(
    string? Name = null,
    string? GoodsNature = null,
    string? Description = null,
    decimal? Price = null);
