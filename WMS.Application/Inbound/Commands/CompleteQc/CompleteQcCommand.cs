using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Inbound.DTOs;
using WMS.Domain.Entities.QcInspectionAggregateRoot;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Commands.CompleteQc;

public sealed record CompleteQcCommand(Guid QcId, CompleteQcRequest Request) : IRequest;

public sealed class CompleteQcCommandHandler(IUnitOfWork uow)
    : IRequestHandler<CompleteQcCommand>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task Handle(CompleteQcCommand request, CancellationToken ct)
    {
        var qcId = request.QcId;
        var req = request.Request;
        var qc = await _uow.Repository<QcInspection>().Query()
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == qcId && !q.IsDeleted, ct)
            ?? throw new AppException(404, "NOT_FOUND", "QC Inspection not found");

        foreach (var reqItem in req.Items)
        {
            var item = qc.Items.FirstOrDefault(i => i.SkuId == reqItem.SkuId);
            if (item != null)
            {
                item.UpdateResults(reqItem.PassedQuantity, reqItem.FailedQuantity, reqItem.Notes);
            }
        }

        qc.CompleteInspection();
        await _uow.SaveChangesAsync(ct);
    }
}
