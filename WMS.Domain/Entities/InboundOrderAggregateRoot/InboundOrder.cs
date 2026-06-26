using WMS.Domain.Common;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities.InboundOrderAggregateRoot;

/// <summary>
/// AKA Purchase Order (PO) or Inbound Shipment.
/// Represents an order placed to a supplier for goods to be received into the warehouse
/// </summary>
public class InboundOrder : BaseEntity, IAggregateRoot
{
    public string OrderNumber { get; set; } = null!;
    public Guid SupplierId { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public DateOnly? ReceivedDate { get; set; }
    public InboundStatus Status { get; set; } = InboundStatus.Pending;
    public decimal TotalValue { get; set; }
    public string? Notes { get; set; }

    public ICollection<InboundItem> Items { get; set; } = [];
}
