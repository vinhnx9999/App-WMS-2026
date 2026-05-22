namespace WMS.Application.Skus.Queries.SearchSkus;

public sealed record SearchSkusResponse(
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
