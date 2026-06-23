namespace WMS.Application.Customers.DTOs;

public sealed record UpdateCustomerResponse(
    Guid Id,
    string Code,
    string Name,
    string? Address,
    string? Phone,
    string? Type,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
