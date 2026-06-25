using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Domain.Interfaces;

namespace WMS.Application.Warehouse.Queries.WarehouseLookup;

public sealed record WarehouseLookupQuery(Guid TenantId) : IRequest<List<WarehouseLookupResponse>>;

public sealed record WarehouseLookupResponse(Guid Id, string Code, string Name);

public sealed class WarehouseLookupQueryHandler(IUnitOfWork uow)
    : IRequestHandler<WarehouseLookupQuery, List<WarehouseLookupResponse>>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<List<WarehouseLookupResponse>> Handle(WarehouseLookupQuery request, CancellationToken ct)
    {
        return await _uow.Repository<Domain.Entities.Warehouses.Warehouse>().Query()
            .AsNoTracking()
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new WarehouseLookupResponse(x.Id, x.Code, x.Name))
            .ToListAsync(ct);
    }
}
