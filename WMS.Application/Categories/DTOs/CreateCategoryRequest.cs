namespace WMS.Application.Categories.DTOs;

public sealed record CreateCategoryRequest(
    string Name,
    string? Description);
