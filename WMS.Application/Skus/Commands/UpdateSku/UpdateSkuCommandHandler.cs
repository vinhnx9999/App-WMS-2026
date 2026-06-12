using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.Product;
using WMS.Domain.Interfaces;

namespace WMS.Application.Skus.Commands.UpdateSku;

public sealed class UpdateSkuCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdateSkuCommand>
{
    public async Task Handle(UpdateSkuCommand request, CancellationToken ct)
    {
        var sku = await uow.Repository<Sku>().Query()
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id
                && x.TenantId == request.TenantId
                && !x.IsDeleted,
                ct);

        if (sku is null)
        {
            throw new AppException(404, "NOT_FOUND", "SKU not found.");
        }

        sku.Update(
            request.Name,
            request.GoodsNature,
            request.Description,
            request.Price);

        await uow.SaveChangesAsync(ct);
    }
}
