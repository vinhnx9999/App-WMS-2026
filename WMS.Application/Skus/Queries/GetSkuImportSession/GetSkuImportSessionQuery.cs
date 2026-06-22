using MediatR;
using WMS.Application.Common.Models;
using WMS.Application.Skus.Commands.ImportSku;

namespace WMS.Application.Skus.Queries.GetSkuImportSession;

public sealed record GetSkuImportSessionQuery(
    Guid TenantId,
    Guid ImportSessionId,
    int Page,
    int Limit)
    : IRequest<GetSkuImportSessionResponse>;

public sealed record GetSkuImportSessionResponse(
    Guid ImportSessionId,
    string? SourceFileName,
    string Status,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    DateTime? CancelledAt,
    DateTime? FailedAt,
    string? FailureReason,
    PagedResult<SkuImportRowPreview> Rows);
