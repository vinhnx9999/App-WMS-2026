namespace WMS.Application.Product.Skus.DTOs;

public sealed record ImportSkusResponse(
    int TotalRows,
    int InsertedRows,
    int FailedRows,
    IReadOnlyList<ImportSkuRowErrorResponse> Errors);

public sealed record ImportSkuRowErrorResponse(
    int RowNumber,
    string? SkuCode,
    string Field,
    string Code,
    string Message);