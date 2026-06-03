using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;

namespace WMS.Application.Product.Skus.Commands.UpdateSku;

public sealed class UpdateSkuCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdateSkuCommand>
{
    public async Task Handle(UpdateSkuCommand request, CancellationToken ct)
    {
        var skuRepo = uow.Repository<Sku>();
        var sku = await skuRepo.Query()
            .Where(x => x.Id == request.Id && x.TenantId == request.TenantId && !x.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (sku is null)
        {
            throw new AppException(404, "NOT_FOUND", "SKU not found");
        }

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await uow.Repository<Category>().Query()
                .AnyAsync(x => x.Id == request.CategoryId.Value && x.TenantId == request.TenantId && !x.IsDeleted, ct);

            if (!categoryExists)
            {
                throw new AppException(400, "INVALID_CATEGORY", "Category not found");
            }

            sku.CategoryId = request.CategoryId.Value;
        }

        if (request.Name is not null)
        {
            sku.Name = request.Name.Trim();
        }

        if (request.Description is not null)
        {
            sku.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        if (request.Price.HasValue)
        {
            sku.ReferencePrice = request.Price.Value;
        }

        await skuRepo.UpdateAsync(sku);
        await uow.SaveChangesAsync(ct);
    }
}
