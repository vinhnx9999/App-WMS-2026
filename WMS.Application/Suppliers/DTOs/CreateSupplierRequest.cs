namespace WMS.Application.Suppliers.DTOs;

public sealed record CreateSupplierRequest(
    string Code,
    string Name,
    string? Contact,
    string? Phone,
    string? Email,
    string? Address);
