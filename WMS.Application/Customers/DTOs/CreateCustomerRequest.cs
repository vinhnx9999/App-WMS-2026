namespace WMS.Application.Customers.DTOs;

public sealed record CreateCustomerRequest(
    string? Code,
    string Name,
    string? Address,
    string? Phone,
    string? Type);
