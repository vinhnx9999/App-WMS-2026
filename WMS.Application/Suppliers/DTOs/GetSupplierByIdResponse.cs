namespace WMS.Application.Suppliers.DTOs;

public sealed record GetSupplierByIdResponse(
    Guid Id,
    string Code,
    string Name,
    string? Contact,
    string? Phone,
    string? Email,
    string? Address,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
