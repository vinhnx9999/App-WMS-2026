using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.Product;
using WMS.Domain.Interfaces;


namespace WMS.Application.Products.Commands.RestoreProduct;

public sealed class RestoreProductCommandHandler(IUnitOfWork uow)
    : IRequestHandler<RestoreProductCommand>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task Handle(RestoreProductCommand request, CancellationToken ct)
    {
        // Load product ignoring query filters to find soft-deleted product
        var product = await _uow.Repository<Product>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (product is null)
        {
            throw new AppException(404, "PRODUCT_NOT_FOUND", "Product not found.");
        }

        if (!product.IsDeleted)
        {
            throw new AppException(400, "PRODUCT_NOT_DELETED", "Only deleted products can be restored.");
        }

        // Restore product
        product.Restore();

        await _uow.SaveChangesAsync(ct);
    }
}
