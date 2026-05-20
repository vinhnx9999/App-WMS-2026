namespace WMS.Application.SignalR.DTOs;

// ─── Zone ───

public record ZoneUtilizationData
{
    public Guid ZoneId { get; init; }
    public string Name { get; init; } = "";
    public string ZoneCode { get; init; } = "";
    public int UsedLocations { get; init; }
    public int TotalLocations { get; init; }
    public decimal UtilizationPct { get; init; }
}
