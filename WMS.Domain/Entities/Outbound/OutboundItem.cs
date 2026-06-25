using WMS.Domain.Common;

namespace WMS.Domain.Entities.Outbound;

public class OutboundItem : BaseEntity
{
    public Guid OutboundOrderId { get; set; }
    public Guid InventoryItemId { get; set; }
    public int Quantity { get; set; }
    public int PickedQuantity { get; set; }

    public OutboundOrder OutboundOrder { get; set; } = null!;
    public string? Note { get; set; }
}