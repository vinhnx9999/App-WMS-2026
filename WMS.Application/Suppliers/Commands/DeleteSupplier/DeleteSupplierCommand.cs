using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.Master;
using WMS.Domain.Interfaces;

namespace WMS.Application.Suppliers.Commands.DeleteSupplier;

public sealed record DeleteSupplierCommand(Guid TenantId, Guid Id) : IRequest;

public sealed class DeleteSupplierCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteSupplierCommand>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task Handle(DeleteSupplierCommand request, CancellationToken ct)
    {
        var supplier = await _uow.Repository<Supplier>().Query()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId && !x.IsDeleted, ct);

        if (supplier == null)
        {
            throw new AppException(404, "SUPPLIER_NOT_FOUND", "Supplier not found.");
        }

        supplier.Delete();
        await _uow.SaveChangesAsync(ct);
    }
}
