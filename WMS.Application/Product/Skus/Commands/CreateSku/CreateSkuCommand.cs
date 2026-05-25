using MediatR;
using WMS.Application.Product.Skus.DTOs;

namespace WMS.Application.Product.Skus.Commands.CreateSku;

public sealed record CreateSkuCommand(
    Guid TenantId,
    string SkuCode,
    Guid? CategoryId,
    string? Name,
    string? Description,
    decimal? Price) : IRequest<CreateSkuResponse>;
