namespace WMS.Application.Categories.DTOs;

public sealed record UpdateCategoryRequest(
    string Name,
    string? Description);
