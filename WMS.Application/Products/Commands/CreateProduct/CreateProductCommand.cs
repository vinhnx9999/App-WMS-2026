using MediatR;
using WMS.Application.Products.DTOs;

namespace WMS.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    Guid TenantId,
    string? ProductCode,
    string ProductName,
    string? Description,
    Guid? CategoryId) : IRequest<CreateProductResponse>;
