using MediatR;

namespace WMS.Application.Product.Skus.Commands.ImportSku;

public sealed record ConfirmSkuImportSessionCommand(
 Guid TenantId,
 Guid ImportSessionId)
 : IRequest<ConfirmSkuImportSessionResponse>;

public sealed record ConfirmSkuImportSessionResponse(
    Guid ImportSessionId,
    string Status,
    int TotalRows,
    int CreatedCount,
    IReadOnlyList<ConfirmedSkuImportItem> CreatedItems);

public sealed record ConfirmedSkuImportItem(
    int RowNumber,
    Guid SkuId,
    Guid ProductId,
    string ProductCode,
    string? ProductName,
    string SkuCode,
    string? Name);
