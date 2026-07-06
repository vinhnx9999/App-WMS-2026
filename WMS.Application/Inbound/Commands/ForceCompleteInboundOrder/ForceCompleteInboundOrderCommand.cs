using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Commands.ForceCompleteInboundOrder;

public sealed record ForceCompleteInboundOrderCommand(Guid TenantId, Guid Id) : IRequest;

public sealed class ForceCompleteInboundOrderCommandHandler(IUnitOfWork uow)
    : IRequestHandler<ForceCompleteInboundOrderCommand>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task Handle(ForceCompleteInboundOrderCommand request, CancellationToken ct)
    {
        var order = await _uow.Repository<InboundOrder>().Query()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId && !x.IsDeleted, ct);

        if (order is null)
        {
            throw new AppException(404, "NOT_FOUND", "Inbound Order not found");
        }

        order.CompleteOrder();

        await _uow.SaveChangesAsync(ct);
    }
}
