using MediatR;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Entities.InboundReceiptAggregateRoot;
using WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;
using WMS.Domain.Entities.ProductAggregateRoot;
using WMS.Domain.Entities.PutawayTaskAggregateRoot;
using WMS.Domain.Entities.QcInspectionAggregateRoot;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Events;
using WMS.Domain.Interfaces;
using WMS.Domain.Orchestrator;
using WMS.Application.Common.Service;

namespace WMS.Application.Inbound.Handlers;

public class InboundWorkflowHandlers(
    IRepository<InboundWorkflowConfig> configRepo,
    IRepository<InboundOrder> inboundOrderRepo,
    IRepository<Sku> skuRepo,
    IRepository<Product> productRepo,
    IRepository<QcInspection> qcRepo,
    IRepository<PutawayTask> putawayRepo,
    InboundWorkflowOrchestrator orchestrator,
    ICurrentUser currentUser,
    ISequenceCodeGenerator codeSequenceGenerator)
    : INotificationHandler<InboundReceiptCompletedEvent>,
      INotificationHandler<QcInspectionCompletedEvent>
{
    public async Task Handle(InboundReceiptCompletedEvent notification, CancellationToken ct)
    {
        var receipt = notification.Receipt;

        // Retrieve supplierId from parent inbound order if present
        Guid? supplierId = null;
        if (receipt.InboundOrderId.HasValue)
        {
            var inboundOrder = await inboundOrderRepo.GetByIdAsync(receipt.InboundOrderId.Value, ct);
            supplierId = inboundOrder?.SupplierId;
        }

        var configs = await configRepo.GetAllAsync(ct);

        // Group receipt items by resolved next step
        var qcItems = new List<InboundReceiptItem>();
        var putawayItems = new List<InboundReceiptItem>();

        foreach (var item in receipt.Items)
        {
            var sku = await skuRepo.GetByIdAsync(item.SkuId, ct);
            Guid? categoryId = null;
            if (sku != null && sku.ProductId.HasValue)
            {
                var product = await productRepo.GetByIdAsync(sku.ProductId.Value, ct);
                categoryId = product?.CategoryId;
            }

            var config = orchestrator.ResolveConfig(receipt.WarehouseId, supplierId, categoryId, configs);
            var nextStep = orchestrator.GetNextStep(config, InboundStepType.Receive);

            if (nextStep == InboundStepType.QC)
            {
                qcItems.Add(item);
            }
            else if (nextStep == InboundStepType.Putaway)
            {
                putawayItems.Add(item);
            }
        }

        // Generate QcInspection if there are items routing to QC
        if (qcItems.Count > 0)
        {
            var tenantId = currentUser.TenantId != Guid.Empty ? currentUser.TenantId : receipt.TenantId;
            var qcNumber = await codeSequenceGenerator.NextAsync(tenantId, CodeSequenceTypes.QcInspection, ct);
            var qcInspection = new QcInspection(
                qcNumber,
                receipt.InboundOrderId,
                receipt.Id,
                receipt.WarehouseId);

            foreach (var item in qcItems)
            {
                qcInspection.AddItem(new QcInspectionItem(item.SkuId, item.ReceivedQuantity, item.ReceivedQuantity, 0, "Auto-created from Receipt"));
            }

            await qcRepo.AddAsync(qcInspection, ct);
        }

        // Generate PutawayTask if there are items routing directly to Putaway
        if (putawayItems.Count > 0)
        {
            var tenantId = currentUser.TenantId != Guid.Empty ? currentUser.TenantId : receipt.TenantId;
            var taskNumber = await codeSequenceGenerator.NextAsync(tenantId, CodeSequenceTypes.PutawayTask, ct);
            var putawayTask = new PutawayTask(
                taskNumber,
                receipt.InboundOrderId,
                receipt.Id,
                null,
                receipt.WarehouseId);

            foreach (var item in putawayItems)
            {
                putawayTask.AddItem(new PutawayTaskItem(item.SkuId, item.ReceivedQuantity, Guid.Empty));
            }

            await putawayRepo.AddAsync(putawayTask, ct);
        }
    }

    public async Task Handle(QcInspectionCompletedEvent notification, CancellationToken ct)
    {
        var inspection = notification.Inspection;

        // Retrieve supplierId from parent inbound order if present
        Guid? supplierId = null;
        if (inspection.InboundOrderId.HasValue)
        {
            var inboundOrder = await inboundOrderRepo.GetByIdAsync(inspection.InboundOrderId.Value, ct);
            supplierId = inboundOrder?.SupplierId;
        }

        var configs = await configRepo.GetAllAsync(ct);

        // Filter items that passed inspection and route to Putaway
        var putawayItems = new List<QcInspectionItem>();

        foreach (var item in inspection.Items.Where(i => i.PassedQuantity > 0))
        {
            var sku = await skuRepo.GetByIdAsync(item.SkuId, ct);
            Guid? categoryId = null;
            if (sku != null && sku.ProductId.HasValue)
            {
                var product = await productRepo.GetByIdAsync(sku.ProductId.Value, ct);
                categoryId = product?.CategoryId;
            }

            var config = orchestrator.ResolveConfig(inspection.WarehouseId, supplierId, categoryId, configs);
            var nextStep = orchestrator.GetNextStep(config, InboundStepType.QC);

            if (nextStep == InboundStepType.Putaway)
            {
                putawayItems.Add(item);
            }
        }

        if (putawayItems.Count > 0)
        {
            var tenantId = currentUser.TenantId != Guid.Empty ? currentUser.TenantId : inspection.TenantId;
            var taskNumber = await codeSequenceGenerator.NextAsync(tenantId, CodeSequenceTypes.PutawayTask, ct);
            var putawayTask = new PutawayTask(
                taskNumber,
                inspection.InboundOrderId,
                inspection.InboundReceiptId,
                inspection.Id,
                inspection.WarehouseId);

            foreach (var item in putawayItems)
            {
                putawayTask.AddItem(new PutawayTaskItem(item.SkuId, item.PassedQuantity, Guid.Empty));
            }

            await putawayRepo.AddAsync(putawayTask, ct);
        }
    }
}
