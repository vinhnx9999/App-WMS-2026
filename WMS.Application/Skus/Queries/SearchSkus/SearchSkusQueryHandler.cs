using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Skus.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;

namespace WMS.Application.Skus.Queries.SearchSkus;

public sealed class SearchSkusQueryHandler(IUnitOfWork uow)
    : IRequestHandler<SearchSkusQuery, PagedResult<SearchSkusResponse>>
{
    public async Task<PagedResult<SearchSkusResponse>> Handle(SearchSkusQuery request, CancellationToken ct)
    {
        var page = Math.Max(request.Page, PaginationDefaults.Page);
        var limit = Math.Clamp(request.Limit, PaginationDefaults.MinLimit, PaginationDefaults.MaxLimit);

        var skus = uow.Repository<Sku>().Query().AsNoTracking()
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted);

        var products = uow.Repository<Domain.Entities.Product.Product>().Query().AsNoTracking()
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted);

        var categories = uow.Repository<Category>().Query().AsNoTracking();

        var query =
            from sku in skus
            join product in products on sku.ProductId equals product.Id into productJoin
            from product in productJoin.DefaultIfEmpty()
            join category in categories on product.CategoryId equals category.Id into catJoin
            from category in catJoin.DefaultIfEmpty()
            select new { Sku = sku, Product = product, Category = category };

        if (request.ProductId.HasValue)
        {
            query = query.Where(x => x.Sku.ProductId == request.ProductId.Value);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(x => x.Product != null && x.Product.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim().ToLower();

            query = query.Where(x =>
                x.Sku.SkuCode.ToLower().Contains(keyword) ||
                (x.Sku.Name != null && x.Sku.Name.ToLower().Contains(keyword)) ||
                (x.Sku.Description != null && x.Sku.Description.ToLower().Contains(keyword)) ||
                (x.Sku.GoodsNature != null && x.Sku.GoodsNature.ToLower().Contains(keyword)) ||
                (x.Product != null && x.Product.ProductCode.ToLower().Contains(keyword)) ||
                (x.Product != null && x.Product.ProductName != null && x.Product.ProductName.ToLower().Contains(keyword)) ||
                (x.Product != null && x.Product.Description != null && x.Product.Description.ToLower().Contains(keyword)));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.Sku.UpdatedAt ?? x.Sku.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new SearchSkusResponse(
                x.Sku.Id,
                x.Sku.TenantId,
                x.Product != null ? x.Product.Id : null,
                x.Product != null ? x.Product.ProductCode : null,
                x.Product != null ? x.Product.ProductName : null,
                x.Product != null ? x.Product.CategoryId : null,
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
