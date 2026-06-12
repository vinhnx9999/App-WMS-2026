using MediatR;
using WMS.Application.Skus.DTOs;

namespace WMS.Application.Skus.Queries.GetSkuById;

public sealed record GetSkuByIdQuery(Guid TenantId, Guid Id) : IRequest<GetSkuByIdResponse>;
