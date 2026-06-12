using MediatR;
using WMS.Application.Common.Models;
using WMS.Application.Products.DTOs;

namespace WMS.Application.Products.Queries.SearchProducts;

public sealed record SearchProductsQuery(
    Guid TenantId,
    string? Search,
    Guid? CategoryId,
    int Page,
    int Limit) : IRequest<PagedResult<SearchProductsResponse>>;
