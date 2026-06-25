using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Common.Service;
using WMS.Application.Products.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Entities.ProductAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;


namespace WMS.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(IUnitOfWork uow, ISequenceCodeGenerator sequenceCodeGenerator)
    : IRequestHandler<CreateProductCommand, CreateProductResponse>
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ISequenceCodeGenerator _sequenceCodeGenerator = sequenceCodeGenerator;

    public async Task<CreateProductResponse> Handle(CreateProductCommand request, CancellationToken ct)
    {
        // 1. Validate product name
        if (string.IsNullOrWhiteSpace(request.ProductName))
        {
            throw new AppException(400, "VALIDATION_FAILED", "Product name is required.");
        }

        // 2. Validate category if provided
        if (request.CategoryId.HasValue && request.CategoryId.Value != Guid.Empty)
        {
            var categoryExists = await _uow.Repository<Category>().Query()
                .AnyAsync(x => x.Id == request.CategoryId.Value && x.TenantId == request.TenantId && !x.IsDeleted, ct);

            if (!categoryExists)
            {
                throw new AppException(404, "CATEGORY_NOT_FOUND", "Category not found.");
            }
        }

        // 3. Resolve Product Code
        string productCode;
        if (string.IsNullOrWhiteSpace(request.ProductCode))
        {
            productCode = await _sequenceCodeGenerator.NextAsync(
                request.TenantId,
                CodeSequenceTypes.Product,
                ct);
        }
        else
        {
            productCode = request.ProductCode.Trim().ToUpperInvariant();

            // Check duplicate (including deleted products to preserve immutable code history)
            var codeExists = await _uow.Repository<Product>().Query()
                .AnyAsync(x => x.ProductCode == productCode && x.TenantId == request.TenantId, ct);

            if (codeExists)
            {
                throw new AppException(409, "DUPLICATE_PRODUCT", "Product code already exists for this tenant.");
            }
        }

        // 4. Create Product
        var product = Product.Create(
            tenantId: request.TenantId,
            productCode: productCode,
            productName: request.ProductName,
            description: request.Description,
            categoryId: request.CategoryId);

        await _uow.Repository<Product>().AddAsync(product, ct);
        await _uow.SaveChangesAsync(ct);

        return new CreateProductResponse(
            product.Id,
            product.TenantId,
            product.ProductCode,
            product.ProductName,
            product.Description,
            product.CategoryId,
            product.CreatedAt,
            product.UpdatedAt);
    }
}
