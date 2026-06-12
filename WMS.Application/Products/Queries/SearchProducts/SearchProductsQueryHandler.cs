using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Products.DTOs;
using WMS.Domain.Interfaces;

namespace WMS.Application.Products.Queries.SearchProducts;

public sealed class SearchProductsQueryHandler(IUnitOfWork uow)
    : IRequestHandler<SearchProductsQuery, PagedResult<SearchProductsResponse>>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<PagedResult<SearchProductsResponse>> Handle(SearchProductsQuery request, CancellationToken ct)
    {
        var page = Math.Max(request.Page, PaginationDefaults.Page);
        var limit = Math.Clamp(request.Limit, PaginationDefaults.MinLimit, PaginationDefaults.MaxLimit);

        var products = _uow.Repository<Domain.Entities.Product.Product>().Query().AsNoTracking()
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted);

        var categories = _uow.Repository<Domain.Entities.Category>().Query().AsNoTracking();

        var query =
            from product in products
            join category in categories on product.CategoryId equals category.Id into catJoin
            from category in catJoin.DefaultIfEmpty()
            select new { Product = product, Category = category };

        if (request.CategoryId.HasValue && request.CategoryId.Value != Guid.Empty)
        {
            query = query.Where(x => x.Product.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim().ToLower();

            query = query.Where(x =>
                x.Product.ProductCode.ToLower().Contains(keyword) ||
                (x.Product.ProductName != null && x.Product.ProductName.ToLower().Contains(keyword)) ||
                (x.Product.Description != null && x.Product.Description.ToLower().Contains(keyword)));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.Product.UpdatedAt ?? x.Product.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new SearchProductsResponse(
                x.Product.Id,
                x.Product.TenantId,
                x.Product.ProductCode,
                x.Product.ProductName,
                x.Product.Description,
                x.Product.CategoryId,
                x.Category != null ? x.Category.Name : null,
                x.Product.CreatedAt,
                x.Product.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<SearchProductsResponse>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = limit
        };
    }
}
