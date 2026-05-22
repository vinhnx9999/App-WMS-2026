namespace WMS.Application.Product.Skus.DTOs;

public sealed record UpdateSkuRequest(
    Guid? CategoryId,
    string? Name,
    string? Description,
    decimal? Price);
