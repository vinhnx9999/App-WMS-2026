using WMS.Domain.Common;

namespace WMS.Domain.Entities.InboundOrderAggregateRoot;

public class InboundItem : BaseEntity
{
    public Guid InboundOrderId { get; set; }
    public Guid SkuId { get; set; }
    public int Quantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public string? Note { get; set; }

    public InboundOrder InboundOrder { get; set; } = null!;
}
