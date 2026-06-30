using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Interfaces;

namespace WMS.Application.Skus.Queries.SkuLookup;

public sealed record SkuLookupQuery(Guid TenantId) : IRequest<List<SkuLookupResponse>>;

public sealed record SkuLookupResponse(Guid Id, string Code, string Name);

public sealed class SkuLookupQueryHandler(IUnitOfWork uow)
    : IRequestHandler<SkuLookupQuery, List<SkuLookupResponse>>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<List<SkuLookupResponse>> Handle(SkuLookupQuery request, CancellationToken ct)
    {
        return await _uow.Repository<Sku>().Query()
            .AsNoTracking()
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted)
            .OrderBy(x => x.SkuCode)
            .Select(x => new SkuLookupResponse(x.Id, x.SkuCode, x.Name ?? ""))
            .ToListAsync(ct);
    }
}
