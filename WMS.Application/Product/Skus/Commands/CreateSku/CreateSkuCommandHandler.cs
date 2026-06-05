using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Common.Service;
using WMS.Application.Product.Skus.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Application.Product.Skus.Commands.CreateSku;

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


        await uow.Repository<Sku>().AddAsync(sku, ct);

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

    private async Task<Domain.Entities.Product.Product> LoadProductReference(
       Guid productId,
       Guid tenantId,
       CancellationToken ct)
    {
        var product = await uow.Repository<Domain.Entities.Product.Product>().Query()
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
        if (!string.IsNullOrWhiteSpace(skuCode))
        {
            return skuCode.Trim().ToUpperInvariant();
        }

        return await sequenceCodeGenerator.NextAsync(
            tenantId,
            CodeSequenceTypes.Sku,
            ct);
    }
}
