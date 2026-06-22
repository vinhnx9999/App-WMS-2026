using MediatR;

namespace WMS.Application.Skus.Commands.ImportSku;

public sealed record CancelSkuImportSessionCommand(
    Guid TenantId,
    Guid ImportSessionId)
    : IRequest<CancelSkuImportSessionResponse>;

public sealed record CancelSkuImportSessionResponse(
    Guid ImportSessionId,
    string Status,
    DateTime CancelledAt);
