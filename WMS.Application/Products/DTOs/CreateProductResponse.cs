namespace WMS.Application.Products.DTOs;

public sealed record CreateProductResponse(
    Guid Id,
    Guid TenantId,
    string ProductCode,
    string? ProductName,
    string? Description,
    Guid? CategoryId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
