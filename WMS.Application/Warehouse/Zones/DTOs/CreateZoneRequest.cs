using WMS.Domain.Enums;

namespace WMS.Application.Warehouse.Zones.DTOs;

public record CreateZoneRequest(
    string Name, string ZoneCode, ZoneType ZoneType,
    int TotalLocations, string? Description);