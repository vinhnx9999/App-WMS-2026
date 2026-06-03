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
        var sku = await uow.Repository<Sku>().Query()
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id
                && x.TenantId == request.TenantId
                && x.DeletedAt == null, ct);

        if (sku is null)
        {
            throw new AppException(404, "NOT_FOUND", "SKU not found.");
        }

        sku.Update(
            name: request.Name,
            goodsNature: request.GoodsNature,
            description: request.Description,
            referencePrice: request.Price);

        await uow.SaveChangesAsync(ct);

        // TODO : break the rule of ddd , this is bad handler, need to refactor
    }
}
