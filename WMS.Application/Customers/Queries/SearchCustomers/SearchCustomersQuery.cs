using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Customers.DTOs;
using WMS.Domain.Entities.Master;
using WMS.Domain.Interfaces;

namespace WMS.Application.Customers.Queries.SearchCustomers;

public sealed record SearchCustomersQuery(
    Guid TenantId,
    string? Search,
    int Page,
    int Limit) : IRequest<PagedResult<SearchCustomersResponse>>;

public sealed class SearchCustomersQueryHandler(IUnitOfWork uow)
    : IRequestHandler<SearchCustomersQuery, PagedResult<SearchCustomersResponse>>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<PagedResult<SearchCustomersResponse>> Handle(SearchCustomersQuery request, CancellationToken ct)
    {
        var page = Math.Max(request.Page, PaginationDefaults.Page);
        var limit = Math.Clamp(request.Limit, PaginationDefaults.MinLimit, PaginationDefaults.MaxLimit);

        var query = _uow.Repository<Customer>().Query().AsNoTracking();

        query = query.Where(x => !x.IsDeleted && x.TenantId == request.TenantId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // skipcq: CS-R1018
            var keyword = request.Search.Trim().ToLower();
            // skipcq: CS-R1018
            query = query.Where(x => x.Code.ToLower().Contains(keyword) || x.Name.ToLower().Contains(keyword));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new SearchCustomersResponse(
                x.Id,
                x.Code,
                x.Name,
                x.Address,
                x.Phone,
                x.Type,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<SearchCustomersResponse>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = limit
        };
    }
}
