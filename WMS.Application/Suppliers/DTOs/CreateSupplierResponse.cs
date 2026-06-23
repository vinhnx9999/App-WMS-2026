namespace WMS.Application.Suppliers.DTOs;

public sealed record CreateSupplierResponse(
    Guid Id,
    string Code,
    string Name,
    string? Contact,
    string? Phone,
    string? Email,
    string? Address,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
