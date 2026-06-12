using MediatR;

namespace WMS.Application.Products.Commands.RestoreProduct;

public sealed record RestoreProductCommand(
    Guid TenantId,
    Guid Id) : IRequest;
