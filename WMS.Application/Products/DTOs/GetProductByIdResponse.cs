namespace WMS.Application.Products.DTOs;

public sealed record GetProductByIdResponse(
    Guid Id,
    Guid TenantId,
    string ProductCode,
    string? ProductName,
    string? Description,
    Guid? CategoryId,
    string? CategoryName,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
