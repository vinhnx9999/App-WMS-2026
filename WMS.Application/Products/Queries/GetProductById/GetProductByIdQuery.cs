using MediatR;
using WMS.Application.Products.DTOs;

namespace WMS.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(
    Guid TenantId,
    Guid Id) : IRequest<GetProductByIdResponse>;
