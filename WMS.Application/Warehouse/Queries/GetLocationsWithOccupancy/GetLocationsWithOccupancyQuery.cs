using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Domain.Entities;
using WMS.Domain.Entities.InventoryAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Application.Warehouse.Queries.GetLocationsWithOccupancy;

public record GetLocationsWithOccupancyQuery(Guid WarehouseId) : IRequest<List<LocationOccupancyDto>>;

public class LocationOccupancyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? CoorX { get; set; }
    public int? CoorY { get; set; }
    public int? CoorZ { get; set; }
    public LocationType Type { get; set; }
    public bool IsBlocked { get; set; }
    public string OccupancyStatus { get; set; } = string.Empty;
}

public sealed class GetLocationsWithOccupancyQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetLocationsWithOccupancyQuery, List<LocationOccupancyDto>>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<List<LocationOccupancyDto>> Handle(
        GetLocationsWithOccupancyQuery request,
        CancellationToken cancellationToken)
    {
        var locations = await _uow.Repository<LocationEntity>()
            .Query()
            .Where(x => x.WarehouseId == request.WarehouseId)
            .ToListAsync(cancellationToken);

        if (locations.Count == 0)
        {
            return [];
        }

        var locationIds = locations.Select(x => x.Id).ToList();

        var occupiedLocationIds = await _uow.Repository<InventoryItem>()
            .Query()
            .Where(x => locationIds.Contains(x.LocationId) && x.Quantity > 0)
            .Select(x => x.LocationId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var occupiedSet = new HashSet<Guid>(occupiedLocationIds);

        return locations.Select(x => new LocationOccupancyDto
        {
            Id = x.Id,
            Name = x.Name,
            CoorX = x.CoorX,
            CoorY = x.CoorY,
            CoorZ = x.CoorZ,
            Type = x.Type,
            IsBlocked = x.IsBlocked,
            OccupancyStatus = GetStatus(x, occupiedSet.Contains(x.Id))
        }).ToList();
    }

    private static string GetStatus(LocationEntity location, bool isOccupied)
    {
        if (location.IsBlocked)
        {
            return "blocked";
        }

        return location.Type switch
        {
            LocationType.HORIZONTAL_PATH => "path",
            LocationType.LIFT_POINT => "lift",
            _ => isOccupied ? "occupied" : "empty"
        };
    }
}
