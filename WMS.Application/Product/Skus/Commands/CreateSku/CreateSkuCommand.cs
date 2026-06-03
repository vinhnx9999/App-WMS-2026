using MediatR;
using WMS.Application.Product.Skus.DTOs;

namespace WMS.Application.Product.Skus.Commands.CreateSku;

/// <summary>
/// Creates a new SKU under an existing Product.
/// </summary>
public sealed record CreateSkuCommand(
    Guid TenantId,
    Guid ProductId,
    string? SkuCode = null,
    string? Name = null,
    string? GoodsNature = null,
    string? Description = null,
    decimal? Price = null) : IRequest<CreateSkuResponse>;
