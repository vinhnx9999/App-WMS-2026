using MediatR;
using WMS.Application.Common.Models;
using WMS.Application.Inbound.DTOs;
using WMS.Domain.Enums;

namespace WMS.Application.Inbound.Queries.SearchInboundOrders;

public record SearchInboundOrdersQuery(
    Guid TenantId,
    string? Search,
    Guid? SupplierId,
    InboundStatus? Status,
    string? SortBy,
    string? SortOrder,
    int Page = PaginationDefaults.Page,
    int Limit = PaginationDefaults.Limit) : IRequest<PagedResult<InboundOrderDto>>;
