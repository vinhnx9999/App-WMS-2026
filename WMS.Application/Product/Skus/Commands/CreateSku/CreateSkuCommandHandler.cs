using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Product.Skus.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;

namespace WMS.Application.Product.Skus.Commands.CreateSku;

/// <summary>
/// Handler
/// </summary>
/// <param name="uow"></param>
public sealed class CreateSkuCommandHandler(IUnitOfWork uow) : IRequestHandler<CreateSkuCommand, CreateSkuResponse>
{
    public async Task<CreateSkuResponse> Handle(CreateSkuCommand request, CancellationToken ct)
    {
        var normalizedSkuCode = ResolveSkuCode(request.SkuCode);
        var normalizedUpper = normalizedSkuCode.ToUpperInvariant();

        var product = await LoadProductAggregate(request.ProductId, request.TenantId, ct);

        await EnsureSkuCodeUnique(request.TenantId, normalizedUpper, ct);

        var sku = product.AddSku(
            request.TenantId,
            normalizedSkuCode,
            request.Name,
            request.GoodsNature,
            request.Description,
            request.Price);

        await uow.SaveChangesAsync(ct);

        return new CreateSkuResponse(
            sku.Id,
            sku.TenantId,
            product.Id,
            product.ProductCode,
            product.ProductName,
            sku.SkuCode,
            sku.Name,
            sku.GoodsNature,
            sku.Description,
            sku.ReferencePrice,
            sku.CreatedAt,
            sku.UpdatedAt);
    }

    private async Task<Domain.Entities.Product.Product> LoadProductAggregate(
        Guid productId, Guid tenantId, CancellationToken ct)
    {
        var product = await uow.Repository<Domain.Entities.Product.Product>().Query()
            .Include(x => x.Skus)
            .FirstOrDefaultAsync(x =>
                x.Id == productId
                && x.TenantId == tenantId
                && !x.IsDeleted, ct);

        if (product is null)
        {
            throw new AppException(404, "PRODUCT_NOT_FOUND", "Product not found.");
        }

        return product;
    }

    private async Task EnsureSkuCodeUnique(
        Guid tenantId, string skuCodeUpper, CancellationToken ct)
    {
        var duplicateExists = await uow.Repository<Sku>().Query()
            .AnyAsync(x =>
                x.TenantId == tenantId
                && !x.IsDeleted
                && x.SkuCode.ToUpper() == skuCodeUpper, ct);

        if (duplicateExists)
        {
            throw new AppException(409, "DUPLICATE_SKU", "SKU code already exists.");
        }
    }

    private static string ResolveSkuCode(string? skuCode)
    {
        if (!string.IsNullOrWhiteSpace(skuCode))
        {
            return skuCode.Trim();
        }

        return $"SKU-{Guid.NewGuid():N}".Substring(0, 14).ToUpperInvariant();
    }
}
