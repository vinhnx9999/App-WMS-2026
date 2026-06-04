using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Interfaces;

namespace WMS.Application.Product.Skus.Commands.DeleteSku;

public sealed class DeleteSkuCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteSkuCommand>
{
    public async Task Handle(DeleteSkuCommand request, CancellationToken ct)
    {
        var product = await LoadProductAggregate(request.Id, request.TenantId, ct);

        product.DeleteSku(request.Id);

        await uow.SaveChangesAsync(ct);
    }

    private async Task<Domain.Entities.Product.Product> LoadProductAggregate(
        Guid skuId, Guid tenantId, CancellationToken ct)
    {
        var product = await uow.Repository<Domain.Entities.Product.Product>().Query()
            .Include(x => x.Skus)
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId
                && !x.IsDeleted
                && x.Skus.Any(s => s.Id == skuId && !s.IsDeleted), ct);

        if (product is null)
        {
            throw new AppException(404, "NOT_FOUND", "SKU not found");
        }

        return product;
    }
}
