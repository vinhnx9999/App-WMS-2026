using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WMS.Application.Common.Models;
using WMS.Application.Inventory.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inventory.Services;

public class InventoryService(IUnitOfWork uow, ICurrentUser user) : IInventoryService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ICurrentUser _user = user;

    public async Task<PagedResult<InventoryDto>> GetListAsync(
        InventoryQuery query, CancellationToken ct)
    {
        var q = _uow.Repository<InventoryItem>().Query()
            .Include(x => x.Sku)
            .Include(x => x.Location)
            .Where(x => !x.IsDeleted);

        // Filters
        if (query.Status.HasValue)
            q = q.Where(x => x.Status == query.Status.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLower();
            q = q.Where(x => x.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                           || (x.SkuCode ?? "").Contains(search, StringComparison.CurrentCultureIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Zone))
            q = q.Where(x => x.Location != null && x.Location.ZoneCode == query.Zone);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(x => x.UpdatedAt)
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .Select(x => new InventoryDto(
                x.Id, x.SkuCode ?? "", x.Name, x.Description,
                x.CategoryName ?? "", x.CategoryId ?? Guid.Empty,
                x.ZoneName ?? "", x.ZoneId ?? Guid.Empty,
                x.LocationName ?? "", x.Quantity, x.MinQuantity,
                x.UnitPrice, x.Status, x.UpdatedAt ?? x.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<InventoryDto>()
        {
            Items = items,
            PageNumber = query.Page,
            PageSize = query.Limit,
            TotalCount = total
        };
    }

    public async Task<InventoryDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var item = await _uow.Repository<InventoryItem>().Query()
            .Include(x => x.Sku)
            .Include(x => x.Location)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Sản phẩm không tồn tại");

        return new InventoryDto(
            item.Id, item.Sku?.SkuCode ?? "", item.Name, item.Description,
            item.CategoryName, item.CategoryId,
            item.Location?.Zone?.Name ?? "", item.Location?.ZoneId,
            item.Location?.Name ?? "", item.Quantity, item.MinQuantity,
            item.UnitPrice, item.Status, item.UpdatedAt ?? item.CreatedAt);
    }

    public async Task<InventoryDto> CreateAsync(
        CreateInventoryRequest request, CancellationToken ct)
    {
        var repo = _uow.Repository<InventoryItem>();

        if (await repo.ExistsAsync(x => x.SkuCode == request.Sku))
            throw new AppException(409, "DUPLICATE", $"SKU '{request.Sku}' đã tồn tại");

        var entity = request.Adapt<InventoryItem>();
        entity.UpdateStatus();

        await repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        await AuditAsync("CREATE", "InventoryItem", entity.Id, null, entity, ct);

        return entity.Adapt<InventoryDto>();
    }

    public async Task UpdateAsync(Guid id, UpdateInventoryRequest request, CancellationToken ct)
    {
        var repo = _uow.Repository<InventoryItem>();
        var entity = await repo.GetByIdAsync(id, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Sản phẩm không tồn tại");

        var oldData = entity.Adapt<InventoryItem>();

        if (request.Name is not null) entity.Name = request.Name;
        if (request.Description is not null) entity.Description = request.Description;
        if (request.SkuId.HasValue) entity.SkuId = request.SkuId;
        if (request.LocationId.HasValue) entity.LocationId = request.LocationId;
        if (request.Quantity.HasValue) entity.Quantity = request.Quantity.Value;
        if (request.MinQuantity.HasValue) entity.MinQuantity = request.MinQuantity.Value;
        if (request.UnitPrice.HasValue) entity.UnitPrice = request.UnitPrice.Value;

        entity.UpdateStatus();

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