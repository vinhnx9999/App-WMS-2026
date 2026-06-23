using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Suppliers.DTOs;
using WMS.Domain.Entities.Master;
using WMS.Domain.Interfaces;

namespace WMS.Application.Suppliers.Queries.SearchSuppliers;

public sealed record SearchSuppliersQuery(
    Guid TenantId,
    string? Search,
    int Page,
    int Limit) : IRequest<PagedResult<SearchSuppliersResponse>>;

public sealed class SearchSuppliersQueryHandler(IUnitOfWork uow)
    : IRequestHandler<SearchSuppliersQuery, PagedResult<SearchSuppliersResponse>>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<PagedResult<SearchSuppliersResponse>> Handle(SearchSuppliersQuery request, CancellationToken ct)
    {
        var page = Math.Max(request.Page, PaginationDefaults.Page);
        var limit = Math.Clamp(request.Limit, PaginationDefaults.MinLimit, PaginationDefaults.MaxLimit);

        var query = _uow.Repository<Supplier>().Query().AsNoTracking();

        query = query.Where(x => !x.IsDeleted && x.TenantId == request.TenantId);


        if (!string.IsNullOrWhiteSpace(request.Search))
        {
#pragma warning disable CS-R1018
            var keyword = request.Search.Trim().ToLower();
            query = query.Where(x => x.Code.ToLower().Contains(keyword) || x.Name.ToLower().Contains(keyword));
#pragma warning restore CS-R1018
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new SearchSuppliersResponse(
                x.Id,
                x.Code,
                x.Name,
                x.Contact,
                x.Phone,
                x.Email,
                x.Address,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<SearchSuppliersResponse>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = limit
        };
    }
}
