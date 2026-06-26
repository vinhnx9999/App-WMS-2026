using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Inbound.DTOs;
using WMS.Domain.Entities.PutawayTaskAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Commands.CompletePutaway;

public sealed record CompletePutawayCommand(Guid PutawayTaskId, CompletePutawayRequest Request) : IRequest;

public sealed class CompletePutawayCommandHandler(IUnitOfWork uow)
    : IRequestHandler<CompletePutawayCommand>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task Handle(CompletePutawayCommand request, CancellationToken ct)
    {
        var putawayTaskId = request.PutawayTaskId;
        var req = request.Request;
        var putaway = await _uow.Repository<PutawayTask>().Query()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == putawayTaskId && !p.IsDeleted, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Putaway Task not found");

        if (putaway.Status == PutawayStatus.Pending || putaway.Status == PutawayStatus.SentToWcs)
        {
            putaway.StartProcessing();
        }

        foreach (var reqItem in req.Items)
        {
            var item = putaway.Items.FirstOrDefault(i => i.SkuId == reqItem.SkuId);
            if (item != null)
            {
                item.CompletePutaway(reqItem.ActualLocationId);
            }
        }

        putaway.CompleteTask();
        await _uow.SaveChangesAsync(ct);
    }
}
