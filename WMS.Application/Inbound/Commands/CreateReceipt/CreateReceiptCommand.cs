using MediatR;
using WMS.Application.Inbound.DTOs;
using WMS.Domain.Entities.InboundReceiptAggregateRoot;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Commands.CreateReceipt;

public sealed record CreateReceiptCommand(CreateReceiptRequest Request) : IRequest<Guid>;

public sealed class CreateReceiptCommandHandler(IUnitOfWork uow)
    : IRequestHandler<CreateReceiptCommand, Guid>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<Guid> Handle(CreateReceiptCommand request, CancellationToken ct)
    {
        var req = request.Request;
        var receipt = new InboundReceipt(
            $"REC-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            req.InboundOrderId,
            req.WarehouseId);

        foreach (var item in req.Items)
        {
            receipt.AddItem(new InboundReceiptItem(
                item.SkuId,
                item.ExpectedQuantity,
                item.ReceivedQuantity,
                item.Notes));
        }

        await _uow.Repository<InboundReceipt>().AddAsync(receipt, ct);
        await _uow.SaveChangesAsync(ct);
        return receipt.Id;
    }
}
