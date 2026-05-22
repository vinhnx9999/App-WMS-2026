using MediatR;

namespace WMS.Application.Product.Skus.Commands.UpdateSku;

public sealed record UpdateSkuCommand(
    Guid TenantId,
    Guid Id,
    Guid? CategoryId,
    string? Name,
    string? Description,
    decimal? Price) : IRequest;
