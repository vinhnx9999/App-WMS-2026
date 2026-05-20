using WMS.Domain.Common;

namespace WMS.Domain.Entities.Inbound;

public class Supplier : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Contact { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public ICollection<InboundOrder> Orders { get; set; } = [];
}
