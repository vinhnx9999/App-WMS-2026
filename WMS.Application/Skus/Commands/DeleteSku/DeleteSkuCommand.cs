using MediatR;

namespace WMS.Application.Skus.Commands.DeleteSku;

public sealed record DeleteSkuCommand(Guid TenantId, Guid Id) : IRequest;
