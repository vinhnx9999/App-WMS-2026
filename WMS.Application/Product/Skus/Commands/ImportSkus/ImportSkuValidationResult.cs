using WMS.Application.Product.Skus.DTOs;

namespace WMS.Application.Product.Skus.Commands.ImportSkus;

public sealed record ImportSkuValidationResult(
    IReadOnlyList<ImportSkuRowInput> Rows,
    IReadOnlyList<ImportSkuRowErrorResponse> Errors)
{
    public bool HasErrors => Errors.Count > 0;
}
