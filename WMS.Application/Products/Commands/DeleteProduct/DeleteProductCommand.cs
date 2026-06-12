using MediatR;

namespace WMS.Application.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid TenantId, Guid Id) : IRequest;
