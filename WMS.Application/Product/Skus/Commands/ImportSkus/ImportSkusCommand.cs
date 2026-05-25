using MediatR;
using WMS.Application.Product.Skus.DTOs;

namespace WMS.Application.Product.Skus.Commands.ImportSkus;

public sealed record ImportSkusCommand(
    Guid TenantId,
    IReadOnlyList<ImportSkuRowInput> Rows,
    ImportSkuMode Mode,
    bool AutoCreateMasterData) : IRequest<ImportSkusResponse>;
