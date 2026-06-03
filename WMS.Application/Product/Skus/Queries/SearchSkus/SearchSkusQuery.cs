using MediatR;
using WMS.Application.Common.Models;
using WMS.Application.Product.Skus.DTOs;

namespace WMS.Application.Product.Skus.Queries.SearchSkus;

public sealed record SearchSkusQuery(
    Guid TenantId,
    string? Search = null,
    Guid? ProductId = null,
    Guid? CategoryId = null,
    int Page = PaginationDefaults.Page,
    int Limit = PaginationDefaults.Limit)
    : IRequest<PagedResult<SearchSkusResponse>>;
