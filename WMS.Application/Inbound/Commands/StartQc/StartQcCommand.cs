using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.QcInspectionAggregateRoot;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Commands.StartQc;

public sealed record StartQcCommand(Guid QcId) : IRequest;

public sealed class StartQcCommandHandler(IUnitOfWork uow)
    : IRequestHandler<StartQcCommand>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task Handle(StartQcCommand request, CancellationToken ct)
    {
        var qcId = request.QcId;
        var qc = await _uow.Repository<QcInspection>().Query()
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == qcId && !q.IsDeleted, ct)
            ?? throw new AppException(404, "NOT_FOUND", "QC Inspection not found");

        qc.StartInspection();
        await _uow.SaveChangesAsync(ct);
    }
}
