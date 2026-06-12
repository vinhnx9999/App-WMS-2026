using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Categories.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;

namespace WMS.Application.Categories.Queries.SearchCategories;

public sealed record SearchCategoriesQuery(
    Guid TenantId,
    string? Search,
    int Page,
    int Limit) : IRequest<PagedResult<SearchCategoriesResponse>>;

public sealed class SearchCategoriesQueryHandler(IUnitOfWork uow)
    : IRequestHandler<SearchCategoriesQuery, PagedResult<SearchCategoriesResponse>>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<PagedResult<SearchCategoriesResponse>> Handle(SearchCategoriesQuery request, CancellationToken ct)
    {
        var page = Math.Max(request.Page, PaginationDefaults.Page);
        var limit = Math.Clamp(request.Limit, PaginationDefaults.MinLimit, PaginationDefaults.MaxLimit);

        var query = _uow.Repository<Category>().Query().AsNoTracking()
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(keyword));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new SearchCategoriesResponse(
                x.Id,
                x.TenantId,
                x.Name,
                x.Slug,
                x.Description,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<SearchCategoriesResponse>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = limit
        };
    }
}
