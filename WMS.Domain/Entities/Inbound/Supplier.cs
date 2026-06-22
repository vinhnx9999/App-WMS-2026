using WMS.Domain.Common;

namespace WMS.Domain.Entities.Inbound;

public class Supplier : BaseEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Contact { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public ICollection<InboundOrder> Orders { get; set; } = [];
}
