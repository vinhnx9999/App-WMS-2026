using MediatR;
using WMS.Application.Inbound.DTOs;

namespace WMS.Application.Inbound.Queries.GetInboundById;

public sealed record GetInboundByIdQuery(Guid TenantId, Guid Id) : IRequest<GetInboundByIdResponse>;
