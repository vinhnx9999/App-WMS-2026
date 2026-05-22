using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;

namespace WMS.Application.Product.Skus.Commands.DeleteSku;

public sealed class DeleteSkuCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteSkuCommand>
{
    public async Task Handle(DeleteSkuCommand request, CancellationToken ct)
    {
        var skuRepo = uow.Repository<SkuEntity>();
        var sku = await skuRepo.Query()
            .Where(x => x.Id == request.Id && x.TenantId == request.TenantId && !x.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (sku is null)
        {
            throw new AppException(404, "NOT_FOUND", "SKU not found");
        }

        await skuRepo.DeleteAsync(sku);
        await uow.SaveChangesAsync(ct);
    }
}
