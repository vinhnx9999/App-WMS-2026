using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Common.Specifications;
using WMS.Application.Product.Skus.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;

namespace WMS.Application.Product.Skus.Queries.SearchSkus;

public sealed class SearchSkusQueryHandler(IUnitOfWork uow)
    : IRequestHandler<SearchSkusQuery, PagedResult<SearchSkusResponse>>
{
    public async Task<PagedResult<SearchSkusResponse>> Handle(SearchSkusQuery request, CancellationToken ct)
    {
        var page = Math.Max(request.Page, PaginationDefaults.Page);
        var limit = Math.Clamp(request.Limit, PaginationDefaults.MinLimit, PaginationDefaults.MaxLimit);

        var specification = new SearchSkusSpecification(
            request.TenantId,
            request.Search,
            request.CategoryId,
            page,
            limit);

        var query = uow.Repository<SkuEntity>().Query().AsNoTracking();
        var countQuery = SpecificationEvaluator.GetQuery(query, specification, false);
        var totalCount = await countQuery.CountAsync(ct);

        var items = await SpecificationEvaluator.GetQuery(query, specification)
            .Select(x => new SearchSkusResponse(
                x.Id,
                x.TenantId,
                x.CategoryId,
                x.Category != null ? x.Category.Name : null,
                x.SkuCode,
                x.Name,
                x.Description,
                x.Price,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<SearchSkusResponse>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = limit
        };
    }
}
