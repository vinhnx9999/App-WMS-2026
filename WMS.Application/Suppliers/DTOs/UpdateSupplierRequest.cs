namespace WMS.Application.Suppliers.DTOs;

public sealed record UpdateSupplierRequest(
    string Name,
    string? Code,
    string? Contact,
    string? Phone,
    string? Email,
    string? Address);
