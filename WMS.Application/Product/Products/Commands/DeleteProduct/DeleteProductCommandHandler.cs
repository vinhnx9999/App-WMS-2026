using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;
using ProductAggregate = WMS.Domain.Entities.Product.Product;

namespace WMS.Application.Product.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await uow.Repository<ProductAggregate>().Query()
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id
                && x.TenantId == request.TenantId
                && !x.IsDeleted,
                ct);

        if (product is null)
        {
            throw new AppException(404, "PRODUCT_NOT_FOUND", "Product not found.");
        }

        var hasActiveSkus = await uow.Repository<Sku>().Query()
            .AnyAsync(x =>
                x.ProductId == request.Id
                && x.TenantId == request.TenantId
                && !x.IsDeleted,
                ct);

        if (hasActiveSkus)
        {
            throw new AppException(409, "CONFLICT", "Product cannot be deleted while active SKUs exist.");
        }

        product.Delete();

        await uow.SaveChangesAsync(ct);
    }
}
