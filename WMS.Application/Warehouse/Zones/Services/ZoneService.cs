using FluentValidation;
using Mapster;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Warehouse.Zones.DTOs;
using WMS.Domain.Entities.Warehouses;
using WMS.Domain.Interfaces;

namespace WMS.Application.Warehouse.Zones.Services;

public class ZoneService(IUnitOfWork uow) : IZoneService
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<List<ZoneDto>> GetAllAsync(CancellationToken ct)
    {
        var zones = await _uow.Repository<Zone>().Query()
            .Include(z => z.Items)
            .Where(z => !z.IsDeleted)
            .OrderBy(z => z.ZoneCode)
            .ToListAsync(ct);

        return [.. zones.Select(z => new ZoneDto(
            z.Id, z.Name, z.ZoneCode, z.ZoneType,
            z.TotalLocations, z.UsedLocations, z.UtilizationPct,
            z.Description, z.Items.Count(i => !i.IsDeleted)
        ))];
    }

    public async Task<ZoneDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var z = await _uow.Repository<Zone>().Query()
            .Include(z => z.Items)
            .FirstOrDefaultAsync(z => z.Id == id && !z.IsDeleted, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Khu vực không tồn tại");

        return new ZoneDto(
            z.Id, z.Name, z.ZoneCode, z.ZoneType,
            z.TotalLocations, z.UsedLocations, z.UtilizationPct,
            z.Description, z.Items.Count(i => !i.IsDeleted));
    }

    public async Task<ZoneDto> CreateAsync(CreateZoneRequest request, CancellationToken ct)
    {
        if (await _uow.Repository<Zone>().ExistsAsync(z => z.ZoneCode == request.ZoneCode))
            throw new AppException(409, "DUPLICATE", $"Mã khu vực '{request.ZoneCode}' đã tồn tại");

        var zone = request.Adapt<Zone>();
        await _uow.Repository<Zone>().AddAsync(zone, ct);
        await _uow.SaveChangesAsync(ct);

        return new ZoneDto(
            zone.Id, zone.Name, zone.ZoneCode, zone.ZoneType,
            zone.TotalLocations, 0, 0, zone.Description, 0);
    }
}
