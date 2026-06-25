using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Interfaces;

namespace WMS.Application.Skus.Commands.ImportSku;

public sealed class UpdateSkuImportRowCommandHandler(
    IUnitOfWork uow)
    : IRequestHandler<UpdateSkuImportRowCommand, UpdateSkuImportRowResponse>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<UpdateSkuImportRowResponse> Handle(
        UpdateSkuImportRowCommand request,
        CancellationToken ct)
    {
        var session = await _uow.Repository<SkuImportSession>().Query()
            .Include(x => x.Rows)
            .FirstOrDefaultAsync(x =>
                x.TenantId == request.TenantId
                && x.Id == request.ImportSessionId
                && !x.IsDeleted, ct);

        if (session == null)
        {
            throw new AppException(
                404,
                "IMPORT_SESSION_NOT_FOUND",
                "Import session not found.");
        }

        // 1. Mutate target row fields
        session.UpdateRow(
            request.ImportRowId,
            request.ProductCode,
            request.SkuCode,
            request.Name,
            request.GoodsNature,
            request.Description,
            request.ReferencePrice);

        // 2. Re-validate all active rows in the session
        var validationInputs = session.Rows
            .Where(x => !x.IsDeleted)
            .Select(x => new SkuImportRowValidationInput
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

        // 3. Mark validity on each row
        foreach (var row in session.Rows.Where(x => !x.IsDeleted))
        {
            if (validationResults.TryGetValue(row.RowNumber, out var validation))
            {
                if (validation.IsValid)
                {
                    row.MarkValid(validation.ProductId!.Value);
                }
                else
                {
                    row.MarkInvalid(validation.ErrorCode!, validation.ErrorMessage ?? "");
                }
            }
        }

        // 4. Recalculate session counters
        session.RecalculateSessionCounters();

        // 5. Save changes
        await _uow.SaveChangesAsync(ct);

        // 6. Map and return response
        return new UpdateSkuImportRowResponse(
            ImportSessionId: session.Id,
            Status: session.Status,
            TotalRows: session.TotalRows,
            ValidRows: session.ValidRows,
            InvalidRows: session.InvalidRows,
            Rows: session.Rows
                .Where(x => !x.IsDeleted)
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
