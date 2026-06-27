using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Inbound.DTOs;
using WMS.Domain.Common;
using WMS.Domain.Entities;
using WMS.Domain.Entities.InventoryAggregateRoot;
using WMS.Domain.Entities.PalletAggregateRoot;
using WMS.Domain.Entities.PutawayTaskAggregateRoot;
using WMS.Domain.Entities.WarehouseAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Events;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Commands.CompletePutaway;

public sealed record CompletePutawayCommand(Guid TenantId, Guid PutawayTaskId, CompletePutawayRequest Request) : IRequest;

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
            .FirstOrDefaultAsync(p => p.Id == putawayTaskId && p.TenantId == request.TenantId && !p.IsDeleted, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Putaway Task not found");

        if (putaway.Status == PutawayStatus.Pending)
        {
            putaway.StartProcessing();
        }

        var wcsMovementItems = new List<WcsMovementItem>();

        foreach (var reqItem in req.Items)
        {
            var item = putaway.Items.FirstOrDefault(i => i.SkuId == reqItem.SkuId);
            if (item != null)
            {
                // Verify target location exists, type is storage slot, and is not blocked
                var location = await _uow.Repository<LocationEntity>().Query()
                    .FirstOrDefaultAsync(l => l.Id == reqItem.ActualLocationId && l.TenantId == request.TenantId && !l.IsDeleted, ct);

                if (location == null)
                {
                    throw new AppException(404, "LOCATION_NOT_FOUND", $"Location not found: {reqItem.ActualLocationId}");
                }

                if (location.IsBlocked)
                {
                    throw new AppException(400, "LOCATION_BLOCKED", $"Location {location.Name} đang bị khóa.");
                }

                if (!location.CanPutway())
                {
                    throw new AppException(400, "INVALID_LOCATION", $"Location {location.Name} cannot be used for putaway.");
                }

                // Check WCS automatic block rules
                var block = await _uow.Repository<Block>().Query()
                    .FirstOrDefaultAsync(b => b.Id == location.BlockId && b.TenantId == request.TenantId && !b.IsDeleted, ct);


                if (block != null && !string.IsNullOrEmpty(block.WcsBlockId))
                {
                    if (string.IsNullOrEmpty(reqItem.PalletCode))
                    {
                        throw new DomainException("PALLET_CODE_REQUIRED", $"PalletCode is required for automated WCS zones. Location {location.Name} belongs to an automated WCS zone.");
                    }

                    var isOccupied = await _uow.Repository<InventoryItem>().Query()
                        .AnyAsync(ii => ii.TenantId == request.TenantId
                                        && ii.LocationId == reqItem.ActualLocationId
                                        && ii.Quantity > 0
                                        && !ii.IsDeleted, ct);
                    if (isOccupied)
                    {
                        throw new AppException(400, "LOCATION_OCCUPIED", $"Location {location.Name} đã có hàng tồn kho.");
                    }

                    wcsMovementItems.Add(new WcsMovementItem(
                        reqItem.PalletCode,
                        location.GetLocationCode(),
                        block.WcsBlockId,
                        location.Id
                    ));
                }

                // Validate serial number constraint: quantity must be 1 if serial is specified
                if (!string.IsNullOrEmpty(reqItem.SerialNumber) && item.PutawayQuantity != 1)
                {
                    throw new AppException(400, "INVALID_QUANTITY", "Quantity must be 1 when Serial Number is specified.");
                }

                // Check serial number uniqueness in active inventory
                if (!string.IsNullOrEmpty(reqItem.SerialNumber))
                {
                    var serialExists = await _uow.Repository<InventoryItem>().Query()
                        .AnyAsync(ii => ii.TenantId == request.TenantId
                                        && ii.SerialNumber == reqItem.SerialNumber
                                        && ii.Quantity > 0
                                        && !ii.IsDeleted, ct);
                    if (serialExists)
                    {
                        throw new AppException(400, "DUPLICATE_SERIAL", "Serial number already exists in active inventory.");
                    }
                }

                Guid? palletId = null;
                if (!string.IsNullOrEmpty(reqItem.PalletCode))
                {
                    var pallet = await _uow.Repository<Pallet>().Query()
                        .FirstOrDefaultAsync(p => p.TenantId == request.TenantId && p.PalletCode == reqItem.PalletCode && !p.IsDeleted, ct);
                    if (pallet == null)
                    {
                        throw new AppException(404, "PALLET_NOT_FOUND", $"Pallet not found: {reqItem.PalletCode}");
                    }

                    // Check mixed SKU rule: if IsMixSku is false, verify no different SKU exists in active stock on this pallet
                    if (!pallet.IsMixSku)
                    {
                        var hasDifferentSku = await _uow.Repository<InventoryItem>().Query()
                            .AnyAsync(ii => ii.TenantId == request.TenantId
                                            && ii.PalletId == pallet.Id
                                            && ii.SkuId != reqItem.SkuId
                                            && ii.Quantity > 0
                                            && !ii.IsDeleted, ct);
                        if (hasDifferentSku)
                        {
                            throw new AppException(400, "MIXED_SKU_NOT_ALLOWED", "Pallet does not allow mixed SKUs.");
                        }
                    }

                    palletId = pallet.Id;
                }

                item.CompletePutaway(
                    reqItem.ActualLocationId,
                    palletId: palletId,
                    supplierId: reqItem.SupplierId,
                    expiryDate: reqItem.ExpiryDate,
                    serialNumber: reqItem.SerialNumber,
                    lotNumber: reqItem.LotNumber);
            }
        }

        if (wcsMovementItems.Count > 0)
        {
            putaway.RequestWcsMovements(wcsMovementItems);
        }

        putaway.CompleteTask();
        await _uow.SaveChangesAsync(ct);
    }
}
