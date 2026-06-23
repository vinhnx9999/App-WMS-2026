namespace WMS.Application.Customers.DTOs;

public sealed record UpdateCustomerRequest(
    string Name,
    string? Address,
    string? Phone,
    string? Type);
