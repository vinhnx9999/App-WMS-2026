using MediatR;

namespace WMS.Application.Skus.Commands.ImportSku;

public sealed record UpdateSkuImportRowCommand(
    Guid TenantId,
    Guid ImportSessionId,
    Guid ImportRowId,
    string? ProductCode,
    string? SkuCode,
    string? Name,
    string? GoodsNature,
    string? Description,
    decimal? ReferencePrice)
    : IRequest<UpdateSkuImportRowResponse>;

public sealed record UpdateSkuImportRowResponse(
    Guid ImportSessionId,
    string Status,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    IReadOnlyList<SkuImportRowPreview> Rows);
