using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Entities.InboundReceiptAggregateRoot;
using WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;
using WMS.Domain.Entities.ProductAggregateRoot;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Interfaces;
using WMS.Domain.Orchestrator;

namespace WMS.Application.Inbound.Commands.CompleteReceipt;

public sealed record CompleteReceiptCommand(Guid ReceiptId) : IRequest;

public sealed class CompleteReceiptCommandHandler(IUnitOfWork uow)
    : IRequestHandler<CompleteReceiptCommand>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task Handle(CompleteReceiptCommand request, CancellationToken ct)
    {
        var receiptId = request.ReceiptId;
        var receipt = await _uow.Repository<InboundReceipt>().Query()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == receiptId && !r.IsDeleted, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Inbound Receipt not found");

        var configs = await _uow.Repository<InboundWorkflowConfig>().GetAllAsync(ct);
        Guid? supplierId = null;
        int expectedPoQty = 0;
        int totalReceivedSoFarAcrossAllReceipts = 0;

        if (receipt.InboundOrderId.HasValue)
        {
            var inboundOrder = await _uow.Repository<InboundOrder>().Query()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == receipt.InboundOrderId.Value, ct);

            if (inboundOrder != null)
            {
                var distinctSuppliers = inboundOrder.Items
                    .Select(i => i.SupplierId)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .Distinct()
                    .ToList();

                supplierId = distinctSuppliers.Count == 1 ? distinctSuppliers[0] : null;
                expectedPoQty = inboundOrder.Items.Sum(i => i.Quantity);

                var otherReceipts = await _uow.Repository<InboundReceipt>().Query()
                    .Include(r => r.Items)
                    .Where(r => r.InboundOrderId == receipt.InboundOrderId.Value && r.Id != receiptId && !r.IsDeleted)
                    .ToListAsync(ct);

                totalReceivedSoFarAcrossAllReceipts = otherReceipts.Sum(r => r.Items.Sum(i => i.ReceivedQuantity))
                                                    + receipt.Items.Sum(i => i.ReceivedQuantity);
            }
        }
        else
        {
            expectedPoQty = receipt.Items.Sum(i => i.ExpectedQuantity);
            totalReceivedSoFarAcrossAllReceipts = receipt.Items.Sum(i => i.ReceivedQuantity);
        }

        Guid? categoryId = null;
        if (receipt.Items.Count > 0)
        {
            var firstItemSkuId = receipt.Items.First().SkuId;
            var sku = await _uow.Repository<Sku>().GetByIdAsync(firstItemSkuId, ct);
            if (sku != null && sku.ProductId.HasValue)
            {
                var product = await _uow.Repository<Product>().GetByIdAsync(sku.ProductId.Value, ct);
                categoryId = product?.CategoryId;
            }
        }

        var orchestrator = new InboundWorkflowOrchestrator();
        var config = orchestrator.ResolveConfig(receipt.WarehouseId, supplierId, categoryId, configs);

        receipt.CompleteReceipt(config, totalReceivedSoFarAcrossAllReceipts, expectedPoQty);
        await _uow.SaveChangesAsync(ct);
    }
}
