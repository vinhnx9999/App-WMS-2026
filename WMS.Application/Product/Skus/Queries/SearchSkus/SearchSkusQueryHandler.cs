using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Product.Skus.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;

namespace WMS.Application.Product.Skus.Queries.SearchSkus;

public sealed class SearchSkusQueryHandler(IUnitOfWork uow)
    : IRequestHandler<SearchSkusQuery, PagedResult<SearchSkusResponse>>
{
    public async Task<PagedResult<SearchSkusResponse>> Handle(SearchSkusQuery request, CancellationToken ct)
    {
        var page = Math.Max(request.Page, PaginationDefaults.Page);
        var limit = Math.Clamp(request.Limit, PaginationDefaults.MinLimit, PaginationDefaults.MaxLimit);

        var skus = uow.Repository<Sku>().Query().AsNoTracking();
        var products = uow.Repository<Domain.Entities.Product.Product>().Query().AsNoTracking();
        var categories = uow.Repository<Category>().Query().AsNoTracking();

        var query =
             from sku in skus
             join product in products
                 on sku.ProductId equals product.Id
             join category in categories
                 on product.CategoryId equals category.Id into categoryJoin
             from category in categoryJoin.DefaultIfEmpty()
             where sku.TenantId == request.TenantId
                   && product.TenantId == request.TenantId
                   && sku.DeletedAt == null
                   && product.DeletedAt == null
             select new
             {
                 Sku = sku,
                 Product = product,
                 Category = category
             };

        if (request.ProductId.HasValue)
        {
            query = query.Where(x => x.Product.Id == request.ProductId.Value);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(x => x.Product.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim().ToLower();

            query = query.Where(x =>
                x.Sku.SkuCode.ToLower().Contains(keyword) ||
                (x.Sku.Name != null && x.Sku.Name.ToLower().Contains(keyword)) ||
                (x.Sku.Description != null && x.Sku.Description.ToLower().Contains(keyword)) ||
                (x.Sku.GoodsNature != null && x.Sku.GoodsNature.ToLower().Contains(keyword)) ||
                 x.Product.ProductCode.ToLower().Contains(keyword) ||
                (x.Product.ProductName != null && x.Product.ProductName.ToLower().Contains(keyword)) ||
                (x.Product.Description != null && x.Product.Description.ToLower().Contains(keyword)));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.Sku.UpdatedAt ?? x.Sku.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new SearchSkusResponse(
                x.Sku.Id,
                x.Sku.TenantId,
                x.Product.Id,
                x.Product.ProductCode,
                x.Product.ProductName,
                x.Product.CategoryId,
                x.Category != null ? x.Category.Name : null,
                x.Sku.SkuCode,
                x.Sku.Name,
                x.Sku.GoodsNature,
                x.Sku.Description,
                x.Sku.ReferencePrice,
                x.Sku.CreatedAt,
                x.Sku.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<SearchSkusResponse>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = limit
        };

    }
}
