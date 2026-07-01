using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Common;
using WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;
using WMS.Domain.Interfaces;
using CategoryEntity = WMS.Domain.Entities.Category;
using SupplierEntity = WMS.Domain.Entities.Master.Supplier;
using WarehouseEntity = WMS.Domain.Entities.WarehouseAggregateRoot.Warehouse;

namespace WMS.Application.Inbound.Commands.CreateInboundWorkflowConfig;

public class CreateInboundWorkflowConfigCommandHandler(IUnitOfWork uow)
: IRequestHandler<CreateInboundWorkflowConfigCommand, Guid>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<Guid> Handle(CreateInboundWorkflowConfigCommand request, CancellationToken ct)
    {
        // 1. Validate Warehouse exists
        var warehouseExists = await _uow.Repository<WarehouseEntity>().Query()
            .AnyAsync(w => w.Id == request.WarehouseId && !w.IsDeleted, ct);
        if (!warehouseExists)
        {
            throw new NotFoundException("Warehouse", request.WarehouseId);
        }

        // 2. Validate Supplier exists if provided
        if (request.SupplierId.HasValue)
        {
            var supplierExists = await _uow.Repository<SupplierEntity>().Query()
                .AnyAsync(s => s.Id == request.SupplierId.Value && !s.IsDeleted, ct);
            if (!supplierExists)
            {
                throw new NotFoundException("Supplier", request.SupplierId.Value);
            }
        }

        // 3. Validate Category exists if provided
        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _uow.Repository<CategoryEntity>().Query()
                .AnyAsync(c => c.Id == request.CategoryId.Value && !c.IsDeleted, ct);
            if (!categoryExists)
            {
                throw new NotFoundException("Category", request.CategoryId.Value);
            }
        }

        // 4. Validate Duplicate Configuration
        var duplicateExists = await _uow.Repository<InboundWorkflowConfig>().Query()
            .AnyAsync(c => c.TenantId == request.TenantId &&
                           c.WarehouseId == request.WarehouseId &&
                           c.SupplierId == request.SupplierId &&
                           c.CategoryId == request.CategoryId &&
                           !c.IsDeleted, ct);

        if (duplicateExists)
        {
            throw new AppException(400, "DUPLICATE_CONFIG", "Cấu hình workflow cho kho hàng/nhà cung cấp/nhóm hàng này đã tồn tại.");
        }

        // 5. Create Config entity
        var config = new InboundWorkflowConfig(
            request.TenantId,
            request.WarehouseId,
            request.SupplierId,
            request.CategoryId,
            request.AllowOverReceive,
            request.OverReceiveTolerancePercentage);

        // 6. Map and Update Steps
        if (request.Steps == null || request.Steps.Count == 0)
        {
            throw new AppException(400, "VALIDATION_FAILED", "Workflow steps cannot be empty.");
        }

        var domainSteps = request.Steps.Select(s => new InboundWorkflowStep(s.StepType, s.Sequence, s.DisplayName)).ToList();
        try
        {
            config.UpdateSteps(domainSteps);
        }
        catch (DomainException ex)
        {
            throw new AppException(400, "VALIDATION_FAILED", ex.Message);
        }

        await _uow.Repository<InboundWorkflowConfig>().AddAsync(config, ct);
        await _uow.SaveChangesAsync(ct);

        return config.Id;
    }
}