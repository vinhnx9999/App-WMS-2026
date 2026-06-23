using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Suppliers.DTOs;
using WMS.Domain.Entities.Master;
using WMS.Domain.Interfaces;

namespace WMS.Application.Suppliers.Commands.CreateSupplier;

public sealed record CreateSupplierCommand(
    Guid TenantId,
    string Code,
    string Name,
    string? Contact,
    string? Phone,
    string? Email,
    string? Address) : IRequest<CreateSupplierResponse>;

public sealed class CreateSupplierCommandHandler(IUnitOfWork uow)
    : IRequestHandler<CreateSupplierCommand, CreateSupplierResponse>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<CreateSupplierResponse> Handle(CreateSupplierCommand request, CancellationToken ct)
    {
        var existing = await _uow.Repository<Supplier>().Query()
            .AnyAsync(x => x.TenantId == request.TenantId && x.Code == request.Code, ct);

        if (existing)
        {
            throw new AppException(400, "SUPPLIER_CODE_ALREADY_EXISTS", "Supplier code is already in use.");
        }

        var supplier = Supplier.Create(
            request.TenantId,
            request.Code,
            request.Name,
            request.Contact,
            request.Phone,
            request.Email,
            request.Address);

        await _uow.Repository<Supplier>().AddAsync(supplier, ct);
        await _uow.SaveChangesAsync(ct);

        return new CreateSupplierResponse(
            supplier.Id,
            supplier.Code,
            supplier.Name,
            supplier.Contact,
            supplier.Phone,
            supplier.Email,
            supplier.Address,
            supplier.CreatedAt,
            supplier.UpdatedAt);
    }
}
