namespace WMS.Application.Skus.DTOs;

public sealed record SearchSkusResponse(
    Guid Id,
    Guid TenantId,
    Guid? ProductId,
    string? ProductCode,
    string? ProductName,
    Guid? CategoryId,
    string? CategoryName,
    string SkuCode,
    string? Name,
    string? GoodsNature,
    string? Description,
    decimal? ReferencePrice,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
