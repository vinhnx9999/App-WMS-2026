using WMS.Domain.Enums;

namespace WMS.Application.Warehouse.Zones.DTOs;

public record ZoneDto(
    Guid Id, string Name, string ZoneCode,
    ZoneType ZoneType, int TotalLocations,
    int UsedLocations, decimal UtilizationPct,
    string? Description, int ItemsCount);
