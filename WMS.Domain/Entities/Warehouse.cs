using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Warehouse : BaseEntity
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Location { get; set; } = null!;

    public string? Address { get; set; }
}
