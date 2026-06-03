namespace WMS.Application.Product.Skus.DTOs;

public sealed record CreateSkuResponse(
    Guid Id,
    Guid TenantId,
    Guid ProductId,
    string ProductCode,
    string? ProductName,
    string SkuCode,
    string? Name,
    string? GoodsNature,
    string? Description,
    decimal? ReferencePrice,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
