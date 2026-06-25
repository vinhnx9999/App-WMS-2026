using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Interfaces;

namespace WMS.Application.Skus.Commands.ImportSku;

public sealed class CancelSkuImportSessionCommandHandler(IUnitOfWork uow)
    : IRequestHandler<CancelSkuImportSessionCommand, CancelSkuImportSessionResponse>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<CancelSkuImportSessionResponse> Handle(
        CancelSkuImportSessionCommand request,
        CancellationToken ct)
    {
        var session = await _uow.Repository<SkuImportSession>().Query()
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

        var cancelledAt = DateTime.UtcNow;
        session.MarkCancelled(cancelledAt);

        await _uow.SaveChangesAsync(ct);

        return new CancelSkuImportSessionResponse(
            ImportSessionId: session.Id,
            Status: session.Status,
            CancelledAt: cancelledAt);
    }
}
