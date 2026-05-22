using MediatR;

namespace WMS.Application.Product.Skus.Commands.DeleteSku;

public sealed record DeleteSkuCommand(Guid TenantId, Guid Id) : IRequest;
