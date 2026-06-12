namespace WMS.Application.Categories.DTOs;

public sealed record GetCategoryByIdResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Slug,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
