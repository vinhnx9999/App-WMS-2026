namespace WMS.Application.Products.DTOs;

public sealed record UpdateProductRequest(
    string ProductName,
    string? Description,
    Guid? CategoryId);
