using MediatR;
using WMS.Application.Common.DynamicSearch;
using WMS.Application.Common.Models;
using WMS.Application.Inbound.DTOs;

namespace WMS.Application.Inbound.Queries.SearchInboundOrders;

public record SearchInboundOrdersQuery(
    Guid TenantId,
    List<SearchObject> SearchObjects,
    int Page = PaginationDefaults.Page,
    int Limit = PaginationDefaults.Limit) : IRequest<PagedResult<InboundOrderDto>>;
