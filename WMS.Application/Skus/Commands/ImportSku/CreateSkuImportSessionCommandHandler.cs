using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.Product;
using WMS.Domain.Interfaces;

namespace WMS.Application.Skus.Commands.ImportSku;

public sealed class CreateSkuImportSessionCommandHandler(
    IUnitOfWork uow)
    : IRequestHandler<CreateSkuImportSessionCommand, CreateSkuImportSessionResponse>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<CreateSkuImportSessionResponse> Handle(
        CreateSkuImportSessionCommand request,
        CancellationToken ct)
    {
        var rows = request.Rows ?? [];

        var session = SkuImportSession.Create(
            tenantId: request.TenantId,
            sourceFileName: request.SourceFileName);

        if (rows.Count == 0)
        {
            session.AddRow(
                rowNumber: 1,
                productCode: null,
                productId: null,
                skuCode: null,
                name: null,
                goodsNature: null,
                description: null,
                referencePrice: null,
                isValid: false,
                errorCode: "EMPTY_IMPORT",
                errorMessage: "Import rows are required.");

            await _uow.Repository<SkuImportSession>().AddAsync(session, ct);
            await _uow.SaveChangesAsync(ct);

            return MapResponse(session);
        }

        ValidateDuplicateRowNumbers(rows);

        var validationInputs = rows.Select(x => new SkuImportRowValidationInput
        {
            RowNumber = x.RowNumber,
            ProductCode = x.ProductCode,
            SkuCode = x.SkuCode,
            Name = x.Name,
            GoodsNature = x.GoodsNature,
            Description = x.Description,
            ReferencePrice = x.ReferencePrice
        }).ToList();

        var validationHelper = new SkuImportValidationHelper(_uow);
        var validationResults = await validationHelper.ValidateRowsAsync(request.TenantId, validationInputs, ct);

        foreach (var row in rows)
        {
            var validation = validationResults[row.RowNumber];

            session.AddRow(
                rowNumber: row.RowNumber,
                productCode: row.ProductCode,
                productId: validation.ProductId,
                skuCode: row.SkuCode,
                name: row.Name,
                goodsNature: row.GoodsNature,
                description: row.Description,
                referencePrice: row.ReferencePrice,
                isValid: validation.IsValid,
                errorCode: validation.ErrorCode,
                errorMessage: validation.ErrorMessage);
        }

        await _uow.Repository<SkuImportSession>().AddAsync(session, ct);
        await _uow.SaveChangesAsync(ct);

        return MapResponse(session);
    }

    private static void ValidateDuplicateRowNumbers(
        IReadOnlyList<ImportSkuRowRequest> rows)
    {
        var duplicateRowNumber = rows
            .GroupBy(x => x.RowNumber)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicateRowNumber is not null)
        {
            throw new AppException(
                400,
                "DUPLICATE_ROW_NUMBER",
                $"Row number {duplicateRowNumber.Key} is duplicated.");
        }
    }

    private static CreateSkuImportSessionResponse MapResponse(
        SkuImportSession session)
    {
        return new CreateSkuImportSessionResponse(
            ImportSessionId: session.Id,
            Status: session.Status,
            TotalRows: session.TotalRows,
            ValidRows: session.ValidRows,
            InvalidRows: session.InvalidRows,
            Rows: session.Rows
                .OrderBy(x => x.RowNumber)
                .Select(x => new SkuImportRowPreview(
                    ImportRowId: x.Id,
                    RowNumber: x.RowNumber,
                    ProductCode: x.ProductCode,
                    ProductId: x.ProductId,
                    SkuCode: x.SkuCode,
                    Name: x.Name,
                    GoodsNature: x.GoodsNature,
                    Description: x.Description,
                    ReferencePrice: x.ReferencePrice,
                    IsValid: x.IsValid,
                    ErrorCode: x.ErrorCode,
                    ErrorMessage: x.ErrorMessage))
                .ToList());
    }
}