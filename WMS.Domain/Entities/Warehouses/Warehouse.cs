using WMS.Domain.Common;

namespace WMS.Domain.Entities.Warehouses;

public class Warehouse : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? Address { get; set; }
}
