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
        var product = await LoadProductAggregate(request.Id, request.TenantId, ct);

        product.UpdateSku(
            request.Id,
            request.Name,
            request.GoodsNature,
            request.Description,
            request.Price);

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
            throw new AppException(404, "NOT_FOUND", "SKU not found.");
        }

        return product;
    }
}
