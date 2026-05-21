using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Common.Specifications;
using WMS.Application.Skus.Dtos;
using WMS.Application.Skus.Specifications;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;

namespace WMS.Application.Skus.Queries.GetSkus;

public sealed class GetSkusQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetSkusQuery, PagedResult<SkuDto>>
{
    public async Task<PagedResult<SkuDto>> Handle(GetSkusQuery request, CancellationToken ct)
    {
        var page = Math.Max(request.Page, 1);
        var limit = Math.Clamp(request.Limit, 1, 100);

        var specification = new SkuSearchSpecification(
            request.TenantId,
            request.Search,
            request.CategoryId,
            page,
            limit);

        var query = uow.Repository<SkuEntity>().Query().AsNoTracking();
        var countQuery = SpecificationEvaluator.GetQuery(query, specification, applyPaging: false);
        var totalCount = await countQuery.CountAsync(ct);

        var items = await SpecificationEvaluator.GetQuery(query, specification)
            .Select(x => new SkuDto(
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

        return new PagedResult<SkuDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = limit
        };
    }
}
