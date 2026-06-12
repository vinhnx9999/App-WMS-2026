namespace WMS.Application.Categories.DTOs;

public sealed record SearchCategoriesResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Slug,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
