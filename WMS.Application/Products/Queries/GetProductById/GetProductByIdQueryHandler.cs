using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Products.DTOs;
using WMS.Domain.Interfaces;


namespace WMS.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetProductByIdQuery, GetProductByIdResponse>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<GetProductByIdResponse> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        var products = _uow.Repository<Domain.Entities.Product.Product>().Query().AsNoTracking()
            .Where(x => x.Id == request.Id && x.TenantId == request.TenantId && !x.IsDeleted);

        var categories = _uow.Repository<Domain.Entities.Category>().Query().AsNoTracking();

        var query =
            from product in products
            join category in categories on product.CategoryId equals category.Id into catJoin
            from category in catJoin.DefaultIfEmpty()
            select new { Product = product, Category = category };

        var result = await query.FirstOrDefaultAsync(ct);

        if (result is null)
        {
            throw new AppException(404, "PRODUCT_NOT_FOUND", "Product not found.");
        }

        return new GetProductByIdResponse(
            result.Product.Id,
            result.Product.TenantId,
            result.Product.ProductCode,
            result.Product.ProductName,
            result.Product.Description,
            result.Product.CategoryId,
            result.Category != null ? result.Category.Name : null,
            result.Product.CreatedAt,
            result.Product.UpdatedAt);
    }
}
