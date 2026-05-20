using WMS.Application.Warehouse.Zones.DTOs;

namespace WMS.Application.Warehouse.Zones.Services;

public interface IZoneService
{
    Task<List<ZoneDto>> GetAllAsync(CancellationToken ct = default);
    Task<ZoneDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ZoneDto> CreateAsync(CreateZoneRequest request, CancellationToken ct = default);
}
