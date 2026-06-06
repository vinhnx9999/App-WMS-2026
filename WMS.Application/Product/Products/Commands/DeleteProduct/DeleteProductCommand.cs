using MediatR;

namespace WMS.Application.Product.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid TenantId, Guid Id) : IRequest;
