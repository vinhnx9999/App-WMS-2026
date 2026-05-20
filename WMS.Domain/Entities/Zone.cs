using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Zone : BaseEntity
{
    public string Name { get; set; } = null!;
    public string ZoneCode { get; set; } = null!;
    public int TotalLocations { get; set; }
    public int UsedLocations { get; set; }
    public ZoneType ZoneType { get; set; } = ZoneType.Standard;
    public string? Description { get; set; }
    public ICollection<InventoryItem> Items { get; set; } = [];

    public decimal UtilizationPct =>
        TotalLocations > 0 ? (decimal)UsedLocations / TotalLocations * 100 : 0;
}
