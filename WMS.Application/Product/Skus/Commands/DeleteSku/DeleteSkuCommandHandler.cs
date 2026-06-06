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

        sku.Delete();

        await uow.SaveChangesAsync(ct);
    }
}
