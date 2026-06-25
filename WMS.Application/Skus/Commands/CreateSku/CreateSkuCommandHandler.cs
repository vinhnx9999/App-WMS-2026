using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Common.Service;
using WMS.Application.Skus.DTOs;
using WMS.Domain.Entities.ProductAggregateRoot;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Application.Skus.Commands.CreateSku;

/// <summary>
/// Handler
/// </summary>
/// <param name="uow"></param>
/// <param name="sequenceCodeGenerator"></param>
public sealed class CreateSkuCommandHandler(IUnitOfWork uow, ISequenceCodeGenerator sequenceCodeGenerator) : IRequestHandler<CreateSkuCommand, CreateSkuResponse>
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ISequenceCodeGenerator _sequenceCodeGenerator = sequenceCodeGenerator;

    public async Task<CreateSkuResponse> Handle(CreateSkuCommand request, CancellationToken ct)
    {
        var product = await LoadProductReference(
            request.ProductId,
            request.TenantId,
            ct);

        var skuCode = await ResolveSkuCode(
            request.TenantId,
            request.SkuCode,
            ct);

        var sku = Sku.Create(
            tenantId: request.TenantId,
            productId: product.Id,
            skuCode: skuCode,
            name: request.Name,
            goodsNature: request.GoodsNature,
            description: request.Description,
            referencePrice: request.Price);

        await _uow.Repository<Sku>().AddAsync(sku, ct);

        await _uow.SaveChangesAsync(ct);

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

    private async Task<Product> LoadProductReference(
       Guid productId,
       Guid tenantId,
       CancellationToken ct)
    {
        var product = await _uow.Repository<Product>().Query()
            .FirstOrDefaultAsync(x =>
                x.Id == productId
                && x.TenantId == tenantId
                && !x.IsDeleted,
                ct);

        if (product is null)
        {
            throw new AppException(
                404,
                "PRODUCT_NOT_FOUND",
                "Product not found.");
        }

        return product;
    }

    private async Task<string> ResolveSkuCode(
       Guid tenantId,
       string? skuCode,
       CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(skuCode))
        {
            return await _sequenceCodeGenerator.NextAsync(
                tenantId,
                CodeSequenceTypes.Sku,
                ct);
        }

        var normalizedSkuCode = NormalizeSkuCode(skuCode);

        await EnsureManualSkuCodeIsUnique(
            tenantId,
            normalizedSkuCode,
            ct);

        return normalizedSkuCode;
    }
    private async Task EnsureManualSkuCodeIsUnique(
      Guid tenantId,
      string skuCode,
      CancellationToken ct)
    {
        var exists = await _uow.Repository<Sku>().Query()
            .AnyAsync(x =>
                x.TenantId == tenantId
                && x.SkuCode == skuCode
                && !x.IsDeleted,
                ct);

        if (exists)
        {
            throw new AppException(
                409,
                "DUPLICATE_SKU",
                "SKU code already exists for this tenant.");
        }
    }

    private static string NormalizeSkuCode(string skuCode)
    {
        if (string.IsNullOrWhiteSpace(skuCode))
        {
            throw new AppException(
                400,
                "SKU_CODE_REQUIRED",
                "SKU code is required.");
        }

        return skuCode.Trim().ToUpperInvariant();
    }
}
