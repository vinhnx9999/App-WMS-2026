using FluentValidation;
using Mapster;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Warehouse.Zones.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Entities.InventoryAggregateRoot;
using WMS.Domain.Interfaces;

namespace WMS.Application.Warehouse.Zones.Services;

public class ZoneService(IUnitOfWork uow, ICurrentUser currentUser) : IZoneService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ICurrentUser _currentUser = currentUser;

    public async Task<List<ZoneDto>> GetAllAsync(CancellationToken ct)
    {
        var zones = await _uow.Repository<Zone>().Query()
            .Where(z => !z.IsDeleted)
            .OrderBy(z => z.ZoneCode)
            .ToListAsync(ct);

        var inventoryLocs = await _uow.Repository<InventoryItem>().Query()
            .Where(i => !i.IsDeleted && i.Quantity > 0)
            .Select(i => i.LocationId)
            .Distinct()
            .ToListAsync(ct);

        var locationStats = await _uow.Repository<LocationEntity>().Query()
            .Where(l => !l.IsDeleted && l.ZoneId != null)
            .Select(l => new
            {
                l.ZoneId,
                l.Id
            })
            .ToListAsync(ct);

        var statsByZone = locationStats
            .GroupBy(l => l.ZoneId!.Value)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Total = g.Count(),
                    Used = g.Count(x => inventoryLocs.Contains(x.Id))
                });

        var itemsCountByZone = await (from item in _uow.Repository<InventoryItem>().Query()
                                      where !item.IsDeleted
                                      join loc in _uow.Repository<LocationEntity>().Query() on item.LocationId equals loc.Id
                                      where !loc.IsDeleted && loc.ZoneId != null
                                      group item by loc.ZoneId into g
                                      select new
                                      {
                                          ZoneId = g.Key!.Value,
                                          Count = g.Count()
                                      })
                                     .ToDictionaryAsync(x => x.ZoneId, x => x.Count, ct);

        return [.. zones.Select(z => {
            statsByZone.TryGetValue(z.Id, out var stats);
            int total = stats?.Total ?? 0;
            int used = stats?.Used ?? 0;
            decimal pct = total > 0 ? (decimal)used / total * 100 : 0;
            itemsCountByZone.TryGetValue(z.Id, out var itemsCount);
            return new ZoneDto(
                z.Id, z.Name, z.ZoneCode, z.ZoneType,
                total, used, pct,
                z.Description, itemsCount
            );
        })];
    }

    public async Task<ZoneDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var z = await _uow.Repository<Zone>().Query()
            .FirstOrDefaultAsync(z => z.Id == id && !z.IsDeleted, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Khu vực không tồn tại");

        var inventoryLocs = await _uow.Repository<InventoryItem>().Query()
            .Where(i => !i.IsDeleted && i.Quantity > 0)
            .Select(i => i.LocationId)
            .Distinct()
            .ToListAsync(ct);

        var locations = await _uow.Repository<LocationEntity>().Query()
            .Where(l => l.ZoneId == id && !l.IsDeleted)
            .Select(l => l.Id)
            .ToListAsync(ct);

        int total = locations.Count;
        int used = locations.Count(x => inventoryLocs.Contains(x));
        decimal pct = total > 0 ? (decimal)used / total * 100 : 0;

        var itemsCount = await (from item in _uow.Repository<InventoryItem>().Query()
                                 where !item.IsDeleted
                                 join loc in _uow.Repository<LocationEntity>().Query() on item.LocationId equals loc.Id
                                 where !loc.IsDeleted && loc.ZoneId == id
                                 select item.Id)
                                .CountAsync(ct);

        return new ZoneDto(
            z.Id, z.Name, z.ZoneCode, z.ZoneType,
            total, used, pct,
            z.Description, itemsCount);
    }

    public async Task<ZoneDto> CreateAsync(CreateZoneRequest request, CancellationToken ct)
    {
        if (await _uow.Repository<Zone>().ExistsAsync(z => z.ZoneCode == request.ZoneCode))
            throw new AppException(409, "DUPLICATE", $"Mã khu vực '{request.ZoneCode}' đã tồn tại");

        var zone = new Zone(_currentUser.TenantId, request.Name, request.ZoneCode, request.ZoneType, request.Description);
        await _uow.Repository<Zone>().AddAsync(zone, ct);
        await _uow.SaveChangesAsync(ct);

        return new ZoneDto(
            zone.Id, zone.Name, zone.ZoneCode, zone.ZoneType,
            0, 0, 0, zone.Description, 0);
    }
}
