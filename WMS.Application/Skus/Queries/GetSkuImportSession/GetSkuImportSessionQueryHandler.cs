using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Skus.Commands.ImportSku;
using WMS.Domain.Entities.Product;
using WMS.Domain.Interfaces;

namespace WMS.Application.Skus.Queries.GetSkuImportSession;

public sealed class GetSkuImportSessionQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetSkuImportSessionQuery, GetSkuImportSessionResponse>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<GetSkuImportSessionResponse> Handle(
        GetSkuImportSessionQuery request,
        CancellationToken ct)
    {
        var session = await _uow.Repository<SkuImportSession>().Query()
            .Include(x => x.Rows)
            .FirstOrDefaultAsync(x =>
                x.Id == request.ImportSessionId
                && x.TenantId == request.TenantId
                && !x.IsDeleted,
                ct);

        if (session is null)
        {
            throw new AppException(
                404,
                "IMPORT_SESSION_NOT_FOUND",
                "Import session not found.");
        }

        var rows = session.Rows
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.RowNumber)
            .Select(x => new SkuImportRowPreview(
                x.Id,
                x.RowNumber,
                x.ProductCode,
                x.ProductId,
                x.SkuCode,
                x.Name,
                x.GoodsNature,
                x.Description,
                x.ReferencePrice,
                x.IsValid,
                x.ErrorCode,
                x.ErrorMessage))
            .ToList();

        return new GetSkuImportSessionResponse(
            ImportSessionId: session.Id,
            SourceFileName: session.SourceFileName,
            Status: session.Status,
            TotalRows: session.TotalRows,
            ValidRows: session.ValidRows,
            InvalidRows: session.InvalidRows,
            CreatedAt: session.CreatedAt,
            ConfirmedAt: session.ConfirmedAt,
            CancelledAt: session.CancelledAt,
            FailedAt: session.FailedAt,
            FailureReason: session.FailureReason,
            Rows: rows);
    }
}
