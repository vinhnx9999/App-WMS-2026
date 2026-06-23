using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.Master;
using WMS.Domain.Interfaces;

namespace WMS.Application.Suppliers.Commands.UpdateSupplier;

public sealed record UpdateSupplierCommand(
    Guid TenantId,
    Guid Id,
    string Name,
    string? Contact,
    string? Phone,
    string? Email,
    string? Address) : IRequest;

public sealed class UpdateSupplierCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdateSupplierCommand>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task Handle(UpdateSupplierCommand request, CancellationToken ct)
    {
        var supplier = await _uow.Repository<Supplier>().Query()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId && !x.IsDeleted, ct);

        if (supplier == null)
        {
            throw new AppException(404, "SUPPLIER_NOT_FOUND", "Supplier not found.");
        }

        supplier.Update(
            request.Name,
            request.Contact,
            request.Phone,
            request.Email,
            request.Address);

        await _uow.SaveChangesAsync(ct);
    }
}
