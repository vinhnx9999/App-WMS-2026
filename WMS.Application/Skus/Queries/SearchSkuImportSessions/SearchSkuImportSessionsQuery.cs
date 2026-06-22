using MediatR;
using WMS.Application.Common.Models;

namespace WMS.Application.Skus.Queries.SearchSkuImportSessions;

public sealed record SearchSkuImportSessionsQuery(
    Guid TenantId,
    string? Status = null,
    int Page = PaginationDefaults.Page,
    int Limit = PaginationDefaults.Limit)
    : IRequest<PagedResult<SearchSkuImportSessionsResponse>>;

public sealed record SearchSkuImportSessionsResponse(
    Guid Id,
    string? SourceFileName,
    string Status,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    DateTime? CancelledAt,
    DateTime? FailedAt,
    string? FailureReason);
