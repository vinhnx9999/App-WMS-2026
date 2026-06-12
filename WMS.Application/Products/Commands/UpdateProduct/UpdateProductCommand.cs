using MediatR;

namespace WMS.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid TenantId,
    Guid Id,
    string ProductName,
    string? Description,
    Guid? CategoryId) : IRequest;
