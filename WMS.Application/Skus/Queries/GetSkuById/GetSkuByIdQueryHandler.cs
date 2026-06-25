using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Skus.DTOs;
using WMS.Domain.Entities.ProductAggregateRoot;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Interfaces;

namespace WMS.Application.Skus.Queries.GetSkuById;

public sealed class GetSkuByIdQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetSkuByIdQuery, GetSkuByIdResponse>
{
    public async Task<GetSkuByIdResponse> Handle(GetSkuByIdQuery request, CancellationToken ct)
    {
        var skus = uow.Repository<Sku>().Query().AsNoTracking()
            .Where(x =>
                x.Id == request.Id
                && x.TenantId == request.TenantId
                && !x.IsDeleted);

        var products = uow.Repository<Product>().Query().AsNoTracking()
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted);

        var result = await (
                from sku in skus
                join product in products on sku.ProductId equals product.Id into productJoin
                from product in productJoin.DefaultIfEmpty()
                select new GetSkuByIdResponse(
                    sku.Id,
                    sku.TenantId,
                    product != null ? product.Id : null,
                    product != null ? product.ProductCode : null,
                    product != null ? product.ProductName : null,
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
