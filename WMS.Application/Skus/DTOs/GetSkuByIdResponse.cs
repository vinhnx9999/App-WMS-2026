namespace WMS.Application.Skus.DTOs;

public sealed record GetSkuByIdResponse(
    Guid Id,
    Guid TenantId,
    Guid? ProductId,
    string? ProductCode,
    string? ProductName,
    string SkuCode,
    string? Name,
    string? GoodsNature,
    string? Description,
    decimal? ReferencePrice,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
