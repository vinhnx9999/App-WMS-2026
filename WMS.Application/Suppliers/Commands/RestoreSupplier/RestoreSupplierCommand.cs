using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.Master;
using WMS.Domain.Interfaces;

namespace WMS.Application.Suppliers.Commands.RestoreSupplier;

public sealed record RestoreSupplierCommand(Guid TenantId, Guid Id) : IRequest;

public sealed class RestoreSupplierCommandHandler(IUnitOfWork uow) : IRequestHandler<RestoreSupplierCommand>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task Handle(RestoreSupplierCommand request, CancellationToken ct)
    {
        var supplier = await _uow.Repository<Supplier>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (supplier == null)
        {
            throw new AppException(404, "SUPPLIER_NOT_FOUND", "Supplier not found.");
        }

        supplier.Restore();
        await _uow.SaveChangesAsync(ct);
    }
}
