using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WMS.Application.Common.Models;
using WMS.Application.Inventory.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Entities.InventoryAggregateRoot;
using WMS.Domain.Entities.Master;
using WMS.Domain.Entities.PalletAggregateRoot;
using WMS.Domain.Entities.ProductAggregateRoot;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inventory.Services;

public class InventoryService(IUnitOfWork uow, ICurrentUser user) : IInventoryService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ICurrentUser _user = user;

    public async Task<PagedResult<InventoryDto>> GetListAsync(
        InventoryQuery query, CancellationToken ct)
    {
        var itemsQuery = from item in _uow.Repository<InventoryItem>().Query().Where(x => !x.IsDeleted)
                         join sku in _uow.Repository<Sku>().Query() on item.SkuId equals sku.Id
                         join prod in _uow.Repository<Product>().Query() on sku.ProductId equals prod.Id into prodGroup
                         from prod in prodGroup.DefaultIfEmpty()
                         join cat in _uow.Repository<Category>().Query() on prod.CategoryId equals cat.Id into catGroup
                         from cat in catGroup.DefaultIfEmpty()
                         join loc in _uow.Repository<LocationEntity>().Query() on item.LocationId equals loc.Id
                         join zone in _uow.Repository<Zone>().Query() on loc.ZoneId equals zone.Id into zoneGroup
                         from zone in zoneGroup.DefaultIfEmpty()
                         join pallet in _uow.Repository<Pallet>().Query() on item.PalletId equals pallet.Id into palletGroup
                         from pallet in palletGroup.DefaultIfEmpty()
                         join supplier in _uow.Repository<Supplier>().Query() on item.SupplierId equals supplier.Id into supplierGroup
                         from supplier in supplierGroup.DefaultIfEmpty()
                         select new { item, sku, cat, loc, zone, pallet, supplier };

        // Filters
        if (query.Status.HasValue)
        {
            itemsQuery = itemsQuery.Where(x => x.item.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLower();
            itemsQuery = itemsQuery.Where(x => (x.sku.Name ?? "").ToLower().Contains(search)
                                           || (x.sku.SkuCode ?? "").ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Zone))
        {
            itemsQuery = itemsQuery.Where(x => x.zone != null && x.zone.ZoneCode == query.Zone);
        }

        var total = await itemsQuery.CountAsync(ct);

        var items = await itemsQuery
            .OrderByDescending(x => x.item.UpdatedAt ?? x.item.CreatedAt)
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .Select(x => new InventoryDto(
                x.item.Id,
                x.sku.SkuCode ?? "",
                x.sku.Name ?? "",
                x.sku.Description ?? "",
                x.cat != null ? x.cat.Name : "",
                x.cat != null ? (Guid?)x.cat.Id : null,
                x.zone != null ? x.zone.Name : "",
                x.zone != null ? (Guid?)x.zone.Id : null,
                x.loc.Name ?? "",
                x.item.Quantity,
                x.item.AllocatedQuantity,
                x.item.AvailableQuantity,
                x.item.UnitPrice,
                x.item.Status,
                x.item.PutawayDate,
                x.item.ExpiryDate,
                x.pallet != null ? (Guid?)x.pallet.Id : null,
                x.pallet != null ? x.pallet.PalletCode : null,
                x.supplier != null ? (Guid?)x.supplier.Id : null,
                x.supplier != null ? x.supplier.Name : null,
                x.item.SerialNumber,
                x.item.UpdatedAt ?? x.item.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<InventoryDto>
        {
            Items = items,
            PageNumber = query.Page,
            PageSize = query.Limit,
            TotalCount = total
        };
    }

    public async Task<InventoryDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var itemData = await (from item in _uow.Repository<InventoryItem>().Query()
                              where item.Id == id && !item.IsDeleted
                              join sku in _uow.Repository<Sku>().Query() on item.SkuId equals sku.Id
                              join prod in _uow.Repository<Product>().Query() on sku.ProductId equals prod.Id into prodGroup
                              from prod in prodGroup.DefaultIfEmpty()
                              join cat in _uow.Repository<Category>().Query() on prod.CategoryId equals cat.Id into catGroup
                              from cat in catGroup.DefaultIfEmpty()
                              join loc in _uow.Repository<LocationEntity>().Query() on item.LocationId equals loc.Id
                              join zone in _uow.Repository<Zone>().Query() on loc.ZoneId equals zone.Id into zoneGroup
                              from zone in zoneGroup.DefaultIfEmpty()
                              join pallet in _uow.Repository<Pallet>().Query() on item.PalletId equals pallet.Id into palletGroup
                              from pallet in palletGroup.DefaultIfEmpty()
                              join supplier in _uow.Repository<Supplier>().Query() on item.SupplierId equals supplier.Id into supplierGroup
                              from supplier in supplierGroup.DefaultIfEmpty()
                              select new { item, sku, cat, loc, zone, pallet, supplier })
                             .FirstOrDefaultAsync(ct);

        if (itemData == null)
            throw new AppException(404, "NOT_FOUND", "Sản phẩm không tồn tại");

        return new InventoryDto(
            itemData.item.Id,
            itemData.sku.SkuCode ?? "",
            itemData.sku.Name ?? "",
            itemData.sku.Description ?? "",
            itemData.cat != null ? itemData.cat.Name : "",
            itemData.cat != null ? (Guid?)itemData.cat.Id : null,
            itemData.zone != null ? itemData.zone.Name : "",
            itemData.zone != null ? (Guid?)itemData.zone.Id : null,
            itemData.loc.Name ?? "",
            itemData.item.Quantity,
            itemData.item.AllocatedQuantity,
            itemData.item.AvailableQuantity,
            itemData.item.UnitPrice,
            itemData.item.Status,
            itemData.item.PutawayDate,
            itemData.item.ExpiryDate,
            itemData.pallet != null ? (Guid?)itemData.pallet.Id : null,
            itemData.pallet != null ? itemData.pallet.PalletCode : null,
            itemData.supplier != null ? (Guid?)itemData.supplier.Id : null,
            itemData.supplier != null ? itemData.supplier.Name : null,
            itemData.item.SerialNumber,
            itemData.item.UpdatedAt ?? itemData.item.CreatedAt);
    }

    public async Task<InventoryDto> CreateAsync(
        CreateInventoryRequest request, CancellationToken ct)
    {
        var repo = _uow.Repository<InventoryItem>();
        var tenantId = _user.TenantId;

        var exists = await repo.ExistsAsync(x =>
            x.TenantId == tenantId &&
            x.SkuId == request.SkuId &&
            x.LocationId == request.LocationId &&
            x.SupplierId == request.SupplierId &&
            x.SerialNumber == request.SerialNumber &&
            x.PalletId == request.PalletId &&
            x.ExpiryDate == request.ExpiryDate &&
            !x.IsDeleted);

        if (exists)
            throw new AppException(409, "DUPLICATE", "Dòng tồn kho với các thuộc tính này đã tồn tại trong kho");

        var skuExists = await _uow.Repository<Sku>().ExistsAsync(x => x.Id == request.SkuId && !x.IsDeleted);
        if (!skuExists)
            throw new AppException(404, "NOT_FOUND", "SKU không tồn tại");

        var locExists = await _uow.Repository<LocationEntity>().ExistsAsync(x => x.Id == request.LocationId && !x.IsDeleted);
        if (!locExists)
            throw new AppException(404, "NOT_FOUND", "Vị trí không tồn tại");

        if (request.PalletId.HasValue)
        {
            var palletExists = await _uow.Repository<Pallet>().ExistsAsync(x => x.Id == request.PalletId.Value && !x.IsDeleted);
            if (!palletExists)
                throw new AppException(404, "NOT_FOUND", "Pallet không tồn tại");
        }

        if (request.SupplierId.HasValue)
        {
            var supplierExists = await _uow.Repository<Supplier>().ExistsAsync(x => x.Id == request.SupplierId.Value && !x.IsDeleted);
            if (!supplierExists)
                throw new AppException(404, "NOT_FOUND", "Nhà cung cấp không tồn tại");
        }

        var entity = InventoryItem.Create(
            tenantId,
            request.SkuId,
            request.LocationId,
            request.SupplierId,
            request.SerialNumber,
            request.PalletId,
            request.Quantity,
            request.UnitPrice,
            request.PutawayDate,
            request.ExpiryDate
        );

        await repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        await AuditAsync("CREATE", "InventoryItem", entity.Id, null, entity, ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task UpdateAsync(Guid id, UpdateInventoryRequest request, CancellationToken ct)
    {
        var repo = _uow.Repository<InventoryItem>();
        var entity = await repo.GetByIdAsync(id, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Sản phẩm không tồn tại");

        var oldData = entity.Adapt<InventoryItem>();

        var skuId = request.SkuId ?? entity.SkuId;
        var locationId = request.LocationId ?? entity.LocationId;
        var supplierId = request.SupplierId.HasValue ? request.SupplierId : entity.SupplierId;
        var serialNumber = request.SerialNumber ?? entity.SerialNumber;
        var palletId = request.PalletId.HasValue ? request.PalletId : entity.PalletId;
        var quantity = request.Quantity ?? entity.Quantity;
        var unitPrice = request.UnitPrice ?? entity.UnitPrice;
        var putawayDate = request.PutawayDate ?? entity.PutawayDate;
        var expiryDate = request.ExpiryDate.HasValue ? request.ExpiryDate : entity.ExpiryDate;

        if (skuId != entity.SkuId ||
            locationId != entity.LocationId ||
            supplierId != entity.SupplierId ||
            serialNumber != entity.SerialNumber ||
            palletId != entity.PalletId ||
            expiryDate != entity.ExpiryDate)
        {
            var exists = await repo.ExistsAsync(x =>
                x.Id != id &&
                x.TenantId == entity.TenantId &&
                x.SkuId == skuId &&
                x.LocationId == locationId &&
                x.SupplierId == supplierId &&
                x.SerialNumber == serialNumber &&
                x.PalletId == palletId &&
                x.ExpiryDate == expiryDate &&
                !x.IsDeleted);

            if (exists)
                throw new AppException(409, "DUPLICATE", "Dòng tồn kho với các thuộc tính này đã tồn tại trong kho");
        }

        if (request.SkuId.HasValue && request.SkuId.Value != entity.SkuId)
        {
            var skuExists = await _uow.Repository<Sku>().ExistsAsync(x => x.Id == request.SkuId.Value && !x.IsDeleted);
            if (!skuExists)
                throw new AppException(404, "NOT_FOUND", "SKU không tồn tại");
        }

        if (request.LocationId.HasValue && request.LocationId.Value != entity.LocationId)
        {
            var locExists = await _uow.Repository<LocationEntity>().ExistsAsync(x => x.Id == request.LocationId.Value && !x.IsDeleted);
            if (!locExists)
                throw new AppException(404, "NOT_FOUND", "Vị trí không tồn tại");
        }

        if (request.PalletId.HasValue && request.PalletId.Value != entity.PalletId)
        {
            var palletExists = await _uow.Repository<Pallet>().ExistsAsync(x => x.Id == request.PalletId.Value && !x.IsDeleted);
            if (!palletExists)
                throw new AppException(404, "NOT_FOUND", "Pallet không tồn tại");
        }

        if (request.SupplierId.HasValue && request.SupplierId.Value != entity.SupplierId)
        {
            var supplierExists = await _uow.Repository<Supplier>().ExistsAsync(x => x.Id == request.SupplierId.Value && !x.IsDeleted);
            if (!supplierExists)
                throw new AppException(404, "NOT_FOUND", "Nhà cung cấp không tồn tại");
        }

        entity.Update(
            skuId,
            locationId,
            supplierId,
            serialNumber,
            palletId,
            quantity,
            unitPrice,
            putawayDate,
            expiryDate
        );

        await _uow.SaveChangesAsync(ct);
        await AuditAsync("UPDATE", "InventoryItem", entity.Id, oldData, entity, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var repo = _uow.Repository<InventoryItem>();
        var entity = await repo.GetByIdAsync(id, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Sản phẩm không tồn tại");

        await repo.DeleteAsync(entity);
        await _uow.SaveChangesAsync(ct);
        await AuditAsync("DELETE", "InventoryItem", entity.Id, entity, null, ct);
    }

    private async Task AuditAsync(string action, string entityName,
        Guid? entityId, object? oldData, object? newData, CancellationToken ct)
    {
        var log = new AuditLog
        {
            UserId = _user.Id,
            Action = action,
            TableName = entityName,
            EntityId = entityId,
            OldValues = oldData != null ? JsonSerializer.Serialize(oldData ?? "") : "",
            NewValues = newData != null ? JsonSerializer.Serialize(newData) : "",
        };
        await _uow.Repository<AuditLog>().AddAsync(log, ct);
    }
}