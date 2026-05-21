namespace WMS.Application.Skus.Dtos;

public sealed record SkuDto(
    Guid Id,
    Guid TenantId,
    Guid? CategoryId,
    string? CategoryName,
    string SkuCode,
    string Name,
    string? Description,
    decimal? Price,
    DateTime CreatedAt,
    DateTime UpdatedAt);
