using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Product.Skus.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;

namespace WMS.Application.Product.Skus.Commands.CreateSku;

public sealed class CreateSkuCommandHandler(IUnitOfWork uow) : IRequestHandler<CreateSkuCommand, CreateSkuResponse>
{
    public async Task<CreateSkuResponse> Handle(CreateSkuCommand request, CancellationToken ct)
    {
        var normalizedSkuCode = request.SkuCode.Trim();
        var normalizedSkuCodeUpper = normalizedSkuCode.ToUpperInvariant();
        var skuRepo = uow.Repository<Sku>();

        var duplicateExists = await skuRepo.Query()
            .AnyAsync(x => x.TenantId == request.TenantId
                && !x.IsDeleted
                && x.SkuCode.ToUpper() == normalizedSkuCodeUpper, ct);

        if (duplicateExists)
        {
            throw new AppException(409, "DUPLICATE_SKU", "SKU code already exists");
        }

        var category = await ResolveCategoryAsync(request, ct);

        var sku = new Sku
        {
            TenantId = request.TenantId,
            CategoryId = category.Id,
            SkuCode = normalizedSkuCode,
            Name = request.Name ?? string.Empty,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            ReferencePrice = request.Price ?? 0m
        };

        await skuRepo.AddAsync(sku, ct);
        await uow.SaveChangesAsync(ct);

        return new CreateSkuResponse(
            sku.Id,
            sku.TenantId,
            sku.CategoryId,
            category.Name,
            sku.SkuCode,
            sku.Name,
            sku.Description,
            sku.ReferencePrice ?? 0m,
            sku.CreatedAt,
            sku.UpdatedAt);
    }

    private async Task<Category> ResolveCategoryAsync(CreateSkuCommand request, CancellationToken ct)
    {
        var categoryRepo = uow.Repository<Category>();

        if (request.CategoryId.HasValue)
        {
            var category = await categoryRepo.Query()
                .FirstOrDefaultAsync(x => x.Id == request.CategoryId.Value
                    && x.TenantId == request.TenantId
                    && !x.IsDeleted, ct);

            return category ?? throw new AppException(400, "INVALID_CATEGORY", "Category not found");
        }

        var defaultCategory = await categoryRepo.Query()
            .FirstOrDefaultAsync(x => x.TenantId == request.TenantId
                && !x.IsDeleted
                && x.Name == SkuDefaults.DefaultCategoryName, ct);

        if (defaultCategory is not null)
        {
            return defaultCategory;
        }

        var categoryToCreate = new Category
        {
            TenantId = request.TenantId,
            Name = SkuDefaults.DefaultCategoryName,
            Slug = SkuDefaults.DefaultCategorySlug
        };

        return await categoryRepo.AddAsync(categoryToCreate, ct);
    }
}
