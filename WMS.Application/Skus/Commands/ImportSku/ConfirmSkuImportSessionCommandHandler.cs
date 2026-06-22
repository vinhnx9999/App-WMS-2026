using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WMS.Application.Common.Models;
using WMS.Application.Common.Service;
using WMS.Domain.Entities.Product;
using WMS.Domain.Enums;
using WMS.Domain.Extensions;
using WMS.Domain.Interfaces;

namespace WMS.Application.Skus.Commands.ImportSku;

public sealed class ConfirmSkuImportSessionCommandHandler(
    IUnitOfWork uow,
    ISequenceCodeGenerator sequenceCodeGenerator,
    ILogger<ConfirmSkuImportSessionCommandHandler> logger)
    : IRequestHandler<ConfirmSkuImportSessionCommand, ConfirmSkuImportSessionResponse>
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ISequenceCodeGenerator _sequenceCodeGenerator = sequenceCodeGenerator;
    private readonly ILogger<ConfirmSkuImportSessionCommandHandler> _logger = logger;

    public async Task<ConfirmSkuImportSessionResponse> Handle(
        ConfirmSkuImportSessionCommand request,
        CancellationToken ct)
    {
        var session = await LoadSession(
            request.TenantId,
            request.ImportSessionId,
            ct);

        ValidateSessionCanBeConfirmed(session);

        var rows = session.Rows
            .Where(x => !x.IsDeleted && x.IsValid)
            .OrderBy(x => x.RowNumber)
            .ToList();

        await RevalidateRowsBeforeConfirm(
            request.TenantId,
            rows,
            ct);

        var productsById = await LoadProductsById(
            request.TenantId,
            rows,
            ct);

        var createdItems = new List<ConfirmedSkuImportItem>();

        foreach (var row in rows)
        {
            var product = productsById[row.ProductId!.Value];

            var skuCode = await ResolveSkuCode(
                request.TenantId,
                row,
                ct);

            _logger.LogInformation("skucode value : {SkuCode}", skuCode);

            var sku = Sku.Create(
                tenantId: request.TenantId,
                productId: product.Id,
                skuCode: skuCode,
                name: row.Name,
                goodsNature: row.GoodsNature,
                description: row.Description,
                referencePrice: row.ReferencePrice);

            await _uow.Repository<Sku>().AddAsync(sku, ct);

            row.AttachCreatedSku(sku.Id);

            createdItems.Add(new ConfirmedSkuImportItem(
                RowNumber: row.RowNumber,
                SkuId: sku.Id,
                ProductId: product.Id,
                ProductCode: product.ProductCode,
                ProductName: product.ProductName,
                SkuCode: sku.SkuCode,
                Name: sku.Name));
        }

        session.MarkConfirmed(DateTime.UtcNow);

        await _uow.SaveChangesAsync(ct);

        return new ConfirmSkuImportSessionResponse(
            ImportSessionId: session.Id,
            Status: session.Status,
            TotalRows: session.TotalRows,
            CreatedCount: createdItems.Count,
            CreatedItems: createdItems);
    }

    private async Task<SkuImportSession> LoadSession(
        Guid tenantId,
        Guid importSessionId,
        CancellationToken ct)
    {
        var session = await _uow.Repository<SkuImportSession>().Query()
            .Include(x => x.Rows)
            .FirstOrDefaultAsync(x =>
                x.Id == importSessionId
                && x.TenantId == tenantId
                && !x.IsDeleted,
                ct);

        if (session is null)
        {
            throw new AppException(
                404,
                "IMPORT_SESSION_NOT_FOUND",
                "Import session not found.");
        }

        return session;
    }

    private static void ValidateSessionCanBeConfirmed(
        SkuImportSession session)
    {
        if (session.Status == SkuImportSessionStatuses.Confirmed)
        {
            throw new AppException(
                409,
                "IMPORT_SESSION_ALREADY_CONFIRMED",
                "Import session has already been confirmed.");
        }

        if (session.Status == SkuImportSessionStatuses.Cancelled)
        {
            throw new AppException(
                409,
                "IMPORT_SESSION_CANCELLED",
                "Cancelled import session cannot be confirmed.");
        }

        if (session.Status == SkuImportSessionStatuses.Failed)
        {
            throw new AppException(
                409,
                "IMPORT_SESSION_FAILED",
                "Failed import session cannot be confirmed.");
        }

        if (session.ValidRows == 0)
        {
            throw new AppException(
                409,
                "IMPORT_SESSION_HAS_NO_VALID_ROWS",
                "Import session has no valid rows to confirm.");
        }
    }

    private async Task RevalidateRowsBeforeConfirm(
        Guid tenantId,
        IReadOnlyList<SkuImportRow> rows,
        CancellationToken ct)
    {
        EnsureAllRowsHaveRequiredResolvedData(rows);

        var productIds = rows
            .Select(x => x.ProductId!.Value)
            .Distinct()
            .ToList();

        var activeProductIds = await _uow.Repository<Domain.Entities.Product.Product>().Query()
            .Where(x =>
                x.TenantId == tenantId
                && productIds.Contains(x.Id)
                && !x.IsDeleted)
            .Select(x => x.Id)
            .ToListAsync(ct);

        var activeProductIdSet = activeProductIds.ToHashSet();

        var missingProductRow = rows.FirstOrDefault(x =>
            x.ProductId is null ||
            !activeProductIdSet.Contains(x.ProductId.Value));

        if (missingProductRow is not null)
        {
            throw new AppException(
                409,
                "PRODUCT_NOT_FOUND",
                $"Product for row {missingProductRow.RowNumber} does not exist.");
        }

        var manualSkuCodes = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.SkuCode))
            .Select(x => Utilities.NormalizeCode(x.SkuCode!))
            .Distinct()
            .ToList();

        if (manualSkuCodes.Count == 0)
        {
            return;
        }

        var existingSkuCode = await _uow.Repository<Sku>().Query()
            .Where(x =>
                x.TenantId == tenantId
                && manualSkuCodes.Contains(x.SkuCode)
                && !x.IsDeleted)
            .Select(x => x.SkuCode)
            .FirstOrDefaultAsync(ct);

        if (existingSkuCode is not null)
        {
            throw new AppException(
                409,
                "DUPLICATE_SKU",
                $"SKU code {existingSkuCode} already exists for this tenant.");
        }
    }

    private static void EnsureAllRowsHaveRequiredResolvedData(
        IReadOnlyList<SkuImportRow> rows)
    {
        var rowMissingProductId = rows.FirstOrDefault(x => x.ProductId is null);

        if (rowMissingProductId is not null)
        {
            throw new AppException(
                409,
                "IMPORT_ROW_PRODUCT_NOT_RESOLVED",
                $"Product is not resolved for row {rowMissingProductId.RowNumber}.");
        }

        var rowMissingName = rows.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Name));

        if (rowMissingName is not null)
        {
            throw new AppException(
                409,
                "SKU_NAME_REQUIRED",
                $"SKU name is required for row {rowMissingName.RowNumber}.");
        }
    }

    private async Task<Dictionary<Guid, Domain.Entities.Product.Product>> LoadProductsById(
        Guid tenantId,
        IReadOnlyList<SkuImportRow> rows,
        CancellationToken ct)
    {
        var productIds = rows
            .Select(x => x.ProductId!.Value)
            .Distinct()
            .ToList();

        return await _uow.Repository<Domain.Entities.Product.Product>().Query()
            .Where(x =>
                x.TenantId == tenantId
                && productIds.Contains(x.Id)
                && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id, x => x, ct);
    }

    private async Task<string> ResolveSkuCode(
        Guid tenantId,
        SkuImportRow row,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(row.SkuCode))
        {
            return (row.SkuCode);
        }

        return await _sequenceCodeGenerator.NextAsync(
            tenantId,
            CodeSequenceTypes.Sku,
            ct);
    }

}