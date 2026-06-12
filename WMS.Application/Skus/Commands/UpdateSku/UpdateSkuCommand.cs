using MediatR;

namespace WMS.Application.Skus.Commands.UpdateSku;

public sealed record UpdateSkuCommand(
    Guid TenantId,
    Guid Id,
    string? Name = null,
    string? GoodsNature = null,
    string? Description = null,
    decimal? Price = null) : IRequest;
