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
                from sku in uow.Repository<SkuEntity>().Query().AsNoTracking()
                where sku.Id == request.Id && sku.TenantId == request.TenantId && !sku.IsDeleted
                join category in uow.Repository<Category>().Query().AsNoTracking()
                    on sku.CategoryId equals category.Id into categories
                from category in categories.DefaultIfEmpty()
                select new GetSkuByIdResponse(
                    sku.Id,
                    sku.TenantId,
                    sku.CategoryId,
                    category != null ? category.Name : null,
                    sku.SkuCode,
                    sku.Name,
                    sku.Description,
                    sku.Price,
                    sku.CreatedAt,
                    sku.UpdatedAt))
            .FirstOrDefaultAsync(ct);

        return result ?? throw new AppException(404, "NOT_FOUND", "SKU not found");
    }
}
