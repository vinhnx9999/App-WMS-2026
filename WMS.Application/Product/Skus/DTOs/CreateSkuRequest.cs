namespace WMS.Application.Product.Skus.DTOs;

public sealed record CreateSkuRequest(
    string SkuCode,
    Guid? CategoryId,
    string? Name,
    string? Description,
    decimal? Price);
