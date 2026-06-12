using WMS.Domain.Common;
using WMS.Domain.Enums;
using WMS.Domain.Extensions;

namespace WMS.Domain.Entities.Product;

public class SkuImportSession : BaseEntity
{
    private readonly List<SkuImportRow> _rows = new();

    private SkuImportSession()
    {
    }

    private SkuImportSession(
        Guid tenantId,
        string? sourceFileName)
    {
        TenantId = tenantId;
        SourceFileName = Utilities.NormalizeNullable(sourceFileName);
        Status = SkuImportSessionStatuses.Validated;
    }

    public string Status { get; private set; } = null!;

    public string? SourceFileName { get; private set; }

    public int TotalRows { get; private set; }

    public int ValidRows { get; private set; }

    public int InvalidRows { get; private set; }

    public DateTime? ConfirmedAt { get; private set; }

    public DateTime? CancelledAt { get; private set; }

    public DateTime? FailedAt { get; private set; }

    public string? FailureReason { get; private set; }

    public IReadOnlyCollection<SkuImportRow> Rows => _rows.AsReadOnly();

    public static SkuImportSession Create(
        Guid tenantId,
        string? sourceFileName)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("TenantId is required.");
        }

        return new SkuImportSession(
            tenantId,
            sourceFileName);
    }

    public void AddRow(
        int rowNumber,
        string? productCode,
        Guid? productId,
        string? skuCode,
        string? name,
        string? goodsNature,
        string? description,
        decimal? referencePrice,
        bool isValid,
        string? errorCode,
        string? errorMessage)
    {
        if (Status != SkuImportSessionStatuses.Validated)
        {
            throw new DomainException(
                "INVALID_IMPORT_SESSION_STATUS",
                "Rows can only be added to a validated import session.");
        }

        var row = SkuImportRow.Create(
            tenantId: TenantId,
            importSessionId: Id,
            rowNumber: rowNumber,
            productCode: productCode,
            productId: productId,
            skuCode: skuCode,
            name: name,
            goodsNature: goodsNature,
            description: description,
            referencePrice: referencePrice,
            isValid: isValid,
            errorCode: errorCode,
            errorMessage: errorMessage);

        _rows.Add(row);

        RecalculateCounters();
    }

    public void MarkConfirmed(DateTime confirmedAt)
    {
        if (Status == SkuImportSessionStatuses.Confirmed)
        {
            throw new DomainException(
                "IMPORT_SESSION_ALREADY_CONFIRMED",
                "Import session has already been confirmed.");
        }

        if (Status == SkuImportSessionStatuses.Cancelled)
        {
            throw new DomainException(
                "IMPORT_SESSION_CANCELLED",
                "Cancelled import session cannot be confirmed.");
        }

        if (Status == SkuImportSessionStatuses.Failed)
        {
            throw new DomainException(
                "IMPORT_SESSION_FAILED",
                "Failed import session cannot be confirmed.");
        }



        Status = SkuImportSessionStatuses.Confirmed;
        ConfirmedAt = confirmedAt;
    }

    public void MarkCancelled(DateTime cancelledAt)
    {
        if (Status == SkuImportSessionStatuses.Confirmed)
        {
            throw new DomainException(
                "IMPORT_SESSION_ALREADY_CONFIRMED",
                "Confirmed import session cannot be cancelled.");
        }

        if (Status == SkuImportSessionStatuses.Cancelled)
        {
            return;
        }

        Status = SkuImportSessionStatuses.Cancelled;
        CancelledAt = cancelledAt;
    }

    public void MarkFailed(
        DateTime failedAt,
        string? failureReason)
    {
        if (Status == SkuImportSessionStatuses.Confirmed)
        {
            throw new DomainException(
                "IMPORT_SESSION_ALREADY_CONFIRMED",
                "Confirmed import session cannot be marked as failed.");
        }

        Status = SkuImportSessionStatuses.Failed;
        FailedAt = failedAt;
        FailureReason = Utilities.NormalizeNullable(failureReason);
    }

    public void AttachCreatedSku(
        int rowNumber,
        Guid skuId)
    {
        if (skuId == Guid.Empty)
        {
            throw new DomainException(
                "INVALID_SKU",
                "SKU id is required.");
        }

        var row = _rows.FirstOrDefault(x => x.RowNumber == rowNumber && !x.IsDeleted);

        if (row is null)
        {
            throw new DomainException(
                "IMPORT_ROW_NOT_FOUND",
                "Import row not found.");
        }

        row.AttachCreatedSku(skuId);
    }

    private void RecalculateCounters()
    {
        TotalRows = _rows.Count(x => !x.IsDeleted);
        ValidRows = _rows.Count(x => !x.IsDeleted && x.IsValid);
        InvalidRows = _rows.Count(x => !x.IsDeleted && !x.IsValid);
    }
}