using MediatR;
using WMS.Application.Common.Models;
using WMS.Application.Skus.Dtos;

namespace WMS.Application.Skus.Queries.GetSkus;

public sealed record GetSkusQuery(
    Guid TenantId,
    string? Search = null,
    Guid? CategoryId = null,
    int Page = 1,
    int Limit = 20) : IRequest<PagedResult<SkuDto>>;
