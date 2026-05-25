namespace WMS.Application.Product.Skus.DTOs;

public sealed record CreateSkuResponse(
    Guid Id,
    Guid TenantId,
    Guid? CategoryId,
    string? CategoryName,
    string SkuCode,
    string Name,
    string? Description,
    decimal Price,
    DateTime CreatedAt,
    DateTime UpdatedAt);
