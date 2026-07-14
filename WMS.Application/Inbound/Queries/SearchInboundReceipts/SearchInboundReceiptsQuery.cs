using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.DynamicSearch;
using WMS.Application.Common.Models;
using WMS.Application.Inbound.DTOs;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Entities.InboundReceiptAggregateRoot;
using WMS.Domain.Entities.WarehouseAggregateRoot;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Queries.SearchInboundReceipts;

public record SearchInboundReceiptsQuery(
    Guid TenantId,
    List<SearchObject> SearchObjects,
    int Page = PaginationDefaults.Page,
    int Limit = PaginationDefaults.Limit) : IRequest<PagedResult<InboundReceiptDto>>;

public class SearchInboundReceiptsQueryHandler(IUnitOfWork uow)
    : IRequestHandler<SearchInboundReceiptsQuery, PagedResult<InboundReceiptDto>>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<PagedResult<InboundReceiptDto>> Handle(SearchInboundReceiptsQuery request, CancellationToken ct)
    {
        var page = Math.Max(request.Page, PaginationDefaults.Page);
        var limit = Math.Clamp(
            request.Limit,
            PaginationDefaults.MinLimit,
            PaginationDefaults.MaxLimit);

        var receiptQuery = _uow.Repository<InboundReceipt>().Query().AsNoTracking()
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted);

        var searchCollection = new QueryCollection();
        if (request.SearchObjects is not null)
        {
            foreach (var searchObject in request.SearchObjects)
            {
                searchCollection.Add(searchObject);
            }

            var dynamicFilter = searchCollection.AsExpression<InboundReceipt>(Condition.OrElse);

            if (dynamicFilter is not null)
            {
                receiptQuery = receiptQuery.Where(dynamicFilter);
            }
        }

        var totalCount = await receiptQuery.CountAsync(ct);

        var query = from receipt in receiptQuery
                    join order in _uow.Repository<InboundOrder>().Query().AsNoTracking()
                        on receipt.InboundOrderId equals order.Id into orders
                    from order in orders.DefaultIfEmpty()
                    join wh in _uow.Repository<WMS.Domain.Entities.WarehouseAggregateRoot.Warehouse>().Query().AsNoTracking()
                        on receipt.WarehouseId equals wh.Id into warehouses
                    from wh in warehouses.DefaultIfEmpty()
                    select new
                    {
                        receipt.Id,
                        receipt.ReceiptNumber,
                        receipt.InboundOrderId,
                        OrderNumber = order != null ? order.OrderNumber : null,
                        receipt.WarehouseId,
                        WarehouseName = wh != null ? wh.Name : null,
                        receipt.Status,
                        receipt.CreatedAt,
                        ItemsCount = receipt.Items.Count
                    };

        var pagedResult = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(ct);

        var items = pagedResult.Select(x => new InboundReceiptDto(
            x.Id,
            x.ReceiptNumber,
            x.InboundOrderId,
            x.OrderNumber,
            x.WarehouseId,
            x.WarehouseName,
            x.Status,
            x.CreatedAt,
            x.ItemsCount
        )).ToList();

        return new PagedResult<InboundReceiptDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = limit
        };
    }
}
