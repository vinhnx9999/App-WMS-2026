using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Inbound.DTOs;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Entities.Master;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Queries.SearchInboundOrders;

public class SearchInboundOrdersQueryHandler(IUnitOfWork uow)
    : IRequestHandler<SearchInboundOrdersQuery, PagedResult<InboundOrderDto>>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<PagedResult<InboundOrderDto>> Handle(SearchInboundOrdersQuery request, CancellationToken ct)
    {
        var orderQuery = _uow.Repository<InboundOrder>().Query()
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted);

        // Filter by supplierId if provided
        if (request.SupplierId.HasValue)
        {
            orderQuery = orderQuery.Where(o => o.Items.Any(i => i.SupplierId == request.SupplierId.Value && !i.IsDeleted));
        }

        // Filter by status if provided
        if (request.Status.HasValue)
        {
            orderQuery = orderQuery.Where(o => o.Status == request.Status.Value);
        }

        // Filter by search string if provided
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();

            // Search on OrderNumber, Notes, or SKU Code/Name of items
            orderQuery = orderQuery.Where(o =>
                o.OrderNumber.ToLower().Contains(searchLower) ||
                (o.Notes != null && o.Notes.ToLower().Contains(searchLower)) ||
                o.Items.Any(i => !i.IsDeleted &&
                    _uow.Repository<Sku>().Query()
                        .Any(s => s.Id == i.SkuId && (
                            (s.SkuCode != null && s.SkuCode.ToLower().Contains(searchLower)) ||
                            (s.Name != null && s.Name.ToLower().Contains(searchLower))
                        ))
                )
            );
        }

        // Apply sorting
        orderQuery = request.SortBy?.ToLower() switch
        {
            "ordernumber" => request.SortOrder?.ToLower() == "desc"
                ? orderQuery.OrderByDescending(o => o.OrderNumber)
                : orderQuery.OrderBy(o => o.OrderNumber),
            "expecteddate" => request.SortOrder?.ToLower() == "desc"
                ? orderQuery.OrderByDescending(o => o.ExpectedDate)
                : orderQuery.OrderBy(o => o.ExpectedDate),
            "status" => request.SortOrder?.ToLower() == "desc"
                ? orderQuery.OrderByDescending(o => o.Status)
                : orderQuery.OrderBy(o => o.Status),
            "totalvalue" => request.SortOrder?.ToLower() == "desc"
                ? orderQuery.OrderByDescending(o => o.TotalValue)
                : orderQuery.OrderBy(o => o.TotalValue),
            _ => orderQuery.OrderByDescending(o => o.CreatedAt) // Default sorting
        };

        var totalCount = await orderQuery.CountAsync(ct);
        var page = request.Page > 0 ? request.Page : PaginationDefaults.Page;
        var limit = request.Limit > 0 ? request.Limit : PaginationDefaults.Limit;

        var pagedOrders = await orderQuery
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(ct);

        var list = new List<InboundOrderDto>();

        foreach (var order in pagedOrders)
        {
            var items = await (from item in _uow.Repository<InboundItem>().Query()
                               where item.InboundOrderId == order.Id && !item.IsDeleted
                               join sku in _uow.Repository<Sku>().Query() on item.SkuId equals sku.Id
                               select new { item, sku })
                               .ToListAsync(ct);

            // Fetch supplier names in batch
            var itemSupplierIds = items.Select(x => x.item.SupplierId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            var suppliers = await _uow.Repository<Supplier>().Query()
                .Where(s => itemSupplierIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name ?? "", ct);

            // Determine master SupplierName
            string supplierName;
            if (itemSupplierIds.Count == 0)
            {
                supplierName = "N/A";
            }
            else if (itemSupplierIds.Count == 1)
            {
                supplierName = suppliers.TryGetValue(itemSupplierIds[0], out var name) ? name : "N/A";
            }
            else
            {
                supplierName = "Nhiều nhà cung cấp";
            }

            var itemDtos = items.Select(x => new InboundItemDto(
                x.sku.SkuCode ?? "",
                x.sku.Name ?? "",
                x.item.Quantity,
                x.item.ReceivedQuantity,
                x.item.SupplierId,
                x.item.SupplierId.HasValue && suppliers.TryGetValue(x.item.SupplierId.Value, out var sName) ? sName : "N/A"
            )).ToList();

            list.Add(new InboundOrderDto(
                order.Id,
                order.OrderNumber,
                supplierName,
                order.ExpectedDate,
                order.Status,
                order.TotalValue,
                itemDtos.Count,
                itemDtos
            ));
        }

        return new PagedResult<InboundOrderDto>
        {
            Items = list,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = limit
        };
    }
}
