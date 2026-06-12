using MediatR;

namespace WMS.Application.Skus.Commands.ImportSku
{
    public sealed record CreateSkuImportSessionCommand(
    Guid TenantId,
    string? SourceFileName,
    IReadOnlyList<ImportSkuRowRequest> Rows)
    : IRequest<CreateSkuImportSessionResponse>;


    public sealed record ImportSkuRowRequest(
        int RowNumber,
        string? ProductCode,
        string? SkuCode,
        string? Name,
        string? GoodsNature,
        string? Description,
        decimal? ReferencePrice);

    public sealed record CreateSkuImportSessionResponse(
    Guid ImportSessionId,
    string Status,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    IReadOnlyList<SkuImportRowPreview> Rows);

    public sealed record SkuImportRowPreview(
    Guid ImportRowId,
    int RowNumber,
    string? ProductCode,
    Guid? ProductId,
    string? SkuCode,
    string? Name,
    string? GoodsNature,
    string? Description,
    decimal? ReferencePrice,
    bool IsValid,
    string? ErrorCode,
    string? ErrorMessage);
}
