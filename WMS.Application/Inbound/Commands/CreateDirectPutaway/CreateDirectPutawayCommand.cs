using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Common.Service;
using WMS.Application.Inbound.DTOs;
using WMS.Domain.Entities.InventoryAggregateRoot;
using WMS.Domain.Entities.PalletAggregateRoot;
using WMS.Domain.Entities.PutawayTaskAggregateRoot;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using WMS.Domain.Services;

namespace WMS.Application.Inbound.Commands.CreateDirectPutaway;

public sealed record CreateDirectPutawayCommand(Guid TenantId, CreateDirectPutawayRequest Request) : IRequest<Guid>;

public sealed class CreateDirectPutawayCommandHandler(
    IUnitOfWork uow,
    ISequenceCodeGenerator sequenceCodeGenerator,
    PalletPutawayDomainService palletPutawayDomainService)
    : IRequestHandler<CreateDirectPutawayCommand, Guid>
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ISequenceCodeGenerator _sequenceCodeGenerator = sequenceCodeGenerator;
    private readonly PalletPutawayDomainService _palletPutawayDomainService = palletPutawayDomainService;

    public async Task<Guid> Handle(CreateDirectPutawayCommand request, CancellationToken ct)
    {
        var tenantId = request.TenantId;
        var req = request.Request;

        var taskCode = await _sequenceCodeGenerator.NextAsync(tenantId, CodeSequenceTypes.PutawayTask, ct);
        var putawayTask = PutawayTask.Create(tenantId, taskCode, null, null, null, req.WarehouseId);

        foreach (var itemReq in req.Items)
        {
            // Check serial number uniqueness in active inventory
            if (!string.IsNullOrEmpty(itemReq.SerialNumber))
            {
                var serialExists = await _uow.Repository<InventoryItem>().Query()
                    .AnyAsync(ii => ii.TenantId == tenantId
                                    && ii.SerialNumber == itemReq.SerialNumber
                                    && ii.Quantity > 0
                                    && !ii.IsDeleted, ct);
                if (serialExists)
                {
                    throw new AppException(400, "DUPLICATE_SERIAL", "Serial number already exists in active inventory.");
                }
            }

            var palletId = await HandlePalletCode(tenantId, itemReq, ct);

            putawayTask.AddItem(
                skuId: itemReq.SkuId,
                putawayQuantity: itemReq.Quantity,
                targetLocationId: itemReq.TargetLocationId,
                actualLocationId: null,
                palletId: palletId,
                supplierId: itemReq.SupplierId,
                expiryDate: itemReq.ExpiryDate,
                serialNumber: itemReq.SerialNumber,
                lotNumber: itemReq.LotNumber);

            putawayTask.CompleteTask();
        }

        await _uow.Repository<PutawayTask>().AddAsync(putawayTask, ct);
        await _uow.SaveChangesAsync(ct);

        return putawayTask.Id;
    }

    private async Task<Guid> HandlePalletCode(
        Guid tenantId,
        CreateDirectPutawayItemRequest itemReq,
        CancellationToken ct)
    {
        var palletCode = itemReq.PalletCode ?? await _sequenceCodeGenerator.NextAsync(tenantId, CodeSequenceTypes.Pallet, ct);

        var pallet = await _uow.Repository<Pallet>().Query()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.PalletCode == palletCode && !p.IsDeleted, ct);

        if (pallet == null)
        {
            pallet = Pallet.Create(tenantId, palletCode);
            await _uow.Repository<Pallet>().AddAsync(pallet, ct);
            await _uow.SaveChangesAsync(ct);
        }

        // Check mixed SKU rule: if IsMixSku is false, verify no different SKU exists in active stock on this pallet
        if (!pallet.IsMixSku)
        {
            var hasDifferentSku = await _uow.Repository<InventoryItem>().Query()
                .AnyAsync(ii => ii.TenantId == tenantId
                                && ii.PalletId == pallet.Id
                                && ii.SkuId != itemReq.SkuId
                                && ii.Quantity > 0
                                && !ii.IsDeleted, ct);
            if (hasDifferentSku)
            {
                throw new AppException(400, "MIXED_SKU_NOT_ALLOWED", "Pallet does not allow mixed SKUs.");
            }
        }

        // Validate pallet capacity constraint
        var sku = await _uow.Repository<Sku>().Query()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == itemReq.SkuId && !s.IsDeleted, ct);
        if (sku == null)
        {
            throw new AppException(404, "SKU_NOT_FOUND", $"SKU not found: {itemReq.SkuId}");
        }

        var currentQuantitiesOnPalletList = await _uow.Repository<InventoryItem>().Query()
            .Where(ii => ii.TenantId == tenantId && ii.PalletId == pallet.Id && ii.Quantity > 0 && !ii.IsDeleted)
            .ToListAsync(ct);

        var currentQuantitiesOnPallet = currentQuantitiesOnPalletList
            .GroupBy(ii => ii.SkuId)
            .ToDictionary(g => g.Key, g => g.Sum(ii => ii.Quantity));

        _palletPutawayDomainService.ValidatePutawayConstraints(pallet, itemReq.SkuId, itemReq.Quantity, sku.MaxQtyInPallet, currentQuantitiesOnPallet);

        return pallet.Id;
    }
}
