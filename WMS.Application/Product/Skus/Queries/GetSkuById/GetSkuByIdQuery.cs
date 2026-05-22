using MediatR;
using WMS.Application.Product.Skus.DTOs;

namespace WMS.Application.Product.Skus.Queries.GetSkuById;

public sealed record GetSkuByIdQuery(Guid TenantId, Guid Id) : IRequest<GetSkuByIdResponse>;
