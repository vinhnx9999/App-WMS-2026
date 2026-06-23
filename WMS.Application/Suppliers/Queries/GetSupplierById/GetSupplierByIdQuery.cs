using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Suppliers.DTOs;
using WMS.Domain.Entities.Master;
using WMS.Domain.Interfaces;

namespace WMS.Application.Suppliers.Queries.GetSupplierById;

public sealed record GetSupplierByIdQuery(Guid TenantId, Guid Id) : IRequest<GetSupplierByIdResponse>;

public sealed class GetSupplierByIdQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetSupplierByIdQuery, GetSupplierByIdResponse>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<GetSupplierByIdResponse> Handle(GetSupplierByIdQuery request, CancellationToken ct)
    {
        var supplier = await _uow.Repository<Supplier>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (supplier == null)
        {
            throw new AppException(404, "SUPPLIER_NOT_FOUND", "Supplier not found.");
        }

        return new GetSupplierByIdResponse(
            supplier.Id,
            supplier.Code,
            supplier.Name,
            supplier.Contact,
            supplier.Phone,
            supplier.Email,
            supplier.Address,
            supplier.IsDeleted,
            supplier.CreatedAt,
            supplier.UpdatedAt);
    }
}
