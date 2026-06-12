namespace WMS.Application.Products.DTOs;

public sealed record CreateProductRequest(
    string? ProductCode,
    string ProductName,
    string? Description,
    Guid? CategoryId);
