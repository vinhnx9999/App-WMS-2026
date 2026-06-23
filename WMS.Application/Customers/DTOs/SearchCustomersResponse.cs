namespace WMS.Application.Customers.DTOs;

public sealed record SearchCustomersResponse(
    Guid Id,
    string Code,
    string Name,
    string? Address,
    string? Phone,
    string? Type,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
