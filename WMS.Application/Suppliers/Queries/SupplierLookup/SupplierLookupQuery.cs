using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Domain.Entities.Master;
using WMS.Domain.Interfaces;

namespace WMS.Application.Suppliers.Queries.SupplierLookup;

public sealed record SupplierLookupQuery(Guid TenantId) : IRequest<List<SupplierLookupResponse>>;

public sealed record SupplierLookupResponse(Guid Id, string Code, string Name);

public sealed class SupplierLookupQueryHandler(IUnitOfWork uow)
    : IRequestHandler<SupplierLookupQuery, List<SupplierLookupResponse>>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<List<SupplierLookupResponse>> Handle(SupplierLookupQuery request, CancellationToken ct)
    {
        return await _uow.Repository<Supplier>().Query()
            .AsNoTracking()
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new SupplierLookupResponse(x.Id, x.Code, x.Name))
            .ToListAsync(ct);
    }
}
