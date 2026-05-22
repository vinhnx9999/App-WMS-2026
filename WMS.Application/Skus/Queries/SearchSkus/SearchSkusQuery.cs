using MediatR;
using WMS.Application.Common.Models;

namespace WMS.Application.Skus.Queries.SearchSkus;

public sealed record SearchSkusQuery(
    Guid TenantId,
    string? Search = null,
    Guid? CategoryId = null,
    int Page = PaginationDefaults.Page,
    int Limit = PaginationDefaults.Limit) : IRequest<PagedResult<SearchSkusResponse>>;
