namespace WMS.Application.Customers.DTOs;

public sealed record CreateCustomerResponse(
    Guid Id,
    string Code,
    string Name,
    string? Address,
    string? Phone,
    string? Type,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
