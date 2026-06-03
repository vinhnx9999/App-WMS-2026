using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Product.Skus.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;

namespace WMS.Application.Product.Skus.Queries.GetSkuById;

public sealed class GetSkuByIdQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetSkuByIdQuery, GetSkuByIdResponse>
{
    public async Task<GetSkuByIdResponse> Handle(GetSkuByIdQuery request, CancellationToken ct)
    {
        var result = await (
                from sku in uow.Repository<Sku>().Query().AsNoTracking()
                where sku.Id == request.Id
                      && sku.TenantId == request.TenantId
                      && sku.DeletedAt == null
                join product in uow.Repository<Domain.Entities.Product.Product>().Query().AsNoTracking()
                    on sku.ProductId equals product.Id
                where product.DeletedAt == null
                select new GetSkuByIdResponse(
                    sku.Id,
                    sku.TenantId,
                    product.Id,
                    product.ProductCode,
                    product.ProductName,
                    sku.SkuCode,
                    sku.Name,
                    sku.GoodsNature,
                    sku.Description,
                    sku.ReferencePrice,
                    sku.CreatedAt,
                    sku.UpdatedAt))
            .FirstOrDefaultAsync(ct);

        return result ?? throw new AppException(404, "NOT_FOUND", "SKU not found.");
    }
}
