using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities.Outbound;

public class OutboundOrder : BaseEntity
{
    public string ShipmentNumber { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public string? Destination { get; set; }
    public DateOnly? ExpectedDelivery { get; set; }
    public OutboundStatus Status { get; set; } = OutboundStatus.Pending;
    public decimal TotalValue { get; set; }
    public Guid? CreatedBy { get; set; }

    public Customer Customer { get; set; } = null!;
    public ICollection<OutboundItem> Items { get; set; } = [];
    public string Notes { get; set; }
}
