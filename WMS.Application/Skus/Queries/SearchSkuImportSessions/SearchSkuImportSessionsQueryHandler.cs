using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.Product;
using WMS.Domain.Interfaces;

namespace WMS.Application.Skus.Queries.SearchSkuImportSessions;

public sealed class SearchSkuImportSessionsQueryHandler(IUnitOfWork uow)
    : IRequestHandler<SearchSkuImportSessionsQuery, PagedResult<SearchSkuImportSessionsResponse>>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<PagedResult<SearchSkuImportSessionsResponse>> Handle(
        SearchSkuImportSessionsQuery request,
        CancellationToken ct)
    {
        var page = Math.Max(request.Page, PaginationDefaults.Page);
        var limit = Math.Clamp(request.Limit, PaginationDefaults.MinLimit, PaginationDefaults.MaxLimit);

        var query = _uow.Repository<SkuImportSession>().Query().AsNoTracking()
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(x => x.Status == request.Status);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new SearchSkuImportSessionsResponse(
                x.Id,
                x.SourceFileName,
                x.Status,
                x.TotalRows,
                x.ValidRows,
                x.InvalidRows,
                x.CreatedAt,
                x.ConfirmedAt,
                x.CancelledAt,
                x.FailedAt,
                x.FailureReason))
            .ToListAsync(ct);

        return new PagedResult<SearchSkuImportSessionsResponse>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = limit
        };
    }
}
