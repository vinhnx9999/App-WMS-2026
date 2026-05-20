using WMS.Domain.Common;

namespace WMS.Domain.Entities.Outbound;

public class Customer : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Type { get; set; }
    public ICollection<OutboundOrder> Orders { get; set; } = [];
}