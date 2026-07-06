using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.DynamicSearch;
using WMS.Application.Common.Models;
using WMS.Application.Inbound.DTOs;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Queries.SearchInboundOrders;

public class SearchInboundOrdersQueryHandler(IUnitOfWork uow)
    : IRequestHandler<SearchInboundOrdersQuery, PagedResult<InboundOrderDto>>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<PagedResult<InboundOrderDto>> Handle(SearchInboundOrdersQuery request, CancellationToken ct)
    {
        var page = Math.Max(request.Page, PaginationDefaults.Page);
        var limit = Math.Clamp(
            request.Limit,
            PaginationDefaults.MinLimit,
            PaginationDefaults.MaxLimit);

        var query = _uow.Repository<InboundOrder>().Query().AsNoTracking()
         .Where(x => x.TenantId == request.TenantId && !x.IsDeleted);

        var searchCollection = new QueryCollection();
        if (request.SearchObjects is not null)
        {
            foreach (var searchObject in request.SearchObjects)
            {
                searchCollection.Add(searchObject);
            }

            var dynamicFilter = searchCollection.AsExpression<InboundOrder>(Condition.OrElse);

            if (dynamicFilter is not null)
            {
                query = query.Where(dynamicFilter);
            }
        }
        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new InboundOrderDto(
                x.Id,
                x.OrderNumber,
                x.ExpectedDate,
                x.Status,
                x.TotalValue,
                x.Items.Count
            ))
            .ToListAsync(ct);

        return new PagedResult<InboundOrderDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = limit
        };
    }
}
