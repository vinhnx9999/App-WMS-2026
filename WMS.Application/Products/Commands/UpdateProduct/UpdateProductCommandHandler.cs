using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities;
using WMS.Domain.Entities.ProductAggregateRoot;
using WMS.Domain.Interfaces;

namespace WMS.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(IUnitOfWork uow)
    : IRequestHandler<UpdateProductCommand>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task Handle(UpdateProductCommand request, CancellationToken ct)
    {
        // 1. Validate product name
        if (string.IsNullOrWhiteSpace(request.ProductName))
        {
            throw new AppException(400, "VALIDATION_FAILED", "Product name is required.");
        }

        // 2. Load product
        var product = await _uow.Repository<Product>().Query()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId && !x.IsDeleted, ct);

        if (product is null)
        {
            throw new AppException(404, "PRODUCT_NOT_FOUND", "Product not found.");
        }

        // 3. Category Validation
        if (product.CategoryId.HasValue)
        {
            // If currently categorized, CategoryId cannot be cleared
            if (!request.CategoryId.HasValue || request.CategoryId.Value == Guid.Empty)
            {
                throw new AppException(400, "CATEGORY_REQUIRED", "Category is required once product is categorized.");
            }
        }

        if (request.CategoryId.HasValue && request.CategoryId.Value != Guid.Empty)
        {
            var categoryExists = await _uow.Repository<Category>().Query()
                .AnyAsync(x => x.Id == request.CategoryId.Value && x.TenantId == request.TenantId && !x.IsDeleted, ct);

            if (!categoryExists)
            {
                throw new AppException(404, "CATEGORY_NOT_FOUND", "Category not found.");
            }
        }

        // 4. Update Product
        product.Update(request.ProductName, request.Description, request.CategoryId);

        await _uow.SaveChangesAsync(ct);
    }
}
