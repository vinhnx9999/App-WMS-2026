using MediatR;
using WMS.Application.Common.Service;
using WMS.Application.Inbound.DTOs;
using WMS.Domain.Entities.InboundReceiptAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Commands.CreateReceipt;

public sealed record CreateReceiptCommand(CreateReceiptRequest Request, Guid TenantId) : IRequest<Guid>;

public sealed class CreateReceiptCommandHandler(IUnitOfWork uow,
    ISequenceCodeGenerator codeSequenceGenerator)
    : IRequestHandler<CreateReceiptCommand, Guid>
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ISequenceCodeGenerator _codeSequenceGenerator = codeSequenceGenerator;

    public async Task<Guid> Handle(CreateReceiptCommand request, CancellationToken ct)
    {
        var req = request.Request;
        var receiptNumber = await _codeSequenceGenerator.NextAsync(request.TenantId, CodeSequenceTypes.InboundReceipt, ct);
        var receipt = new InboundReceipt(
            receiptNumber,
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
