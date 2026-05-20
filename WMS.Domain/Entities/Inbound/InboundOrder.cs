using WMS.Domain.Common;
using WMS.Domain.Entities.Security;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities.Inbound;

public class InboundOrder : BaseEntity
{
    public string OrderNumber { get; set; } = null!;
    public Guid SupplierId { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public DateOnly? ReceivedDate { get; set; }
    public InboundStatus Status { get; set; } = InboundStatus.Pending;
    public decimal TotalValue { get; set; }
    public string? Notes { get; set; }
    public Guid? CreatedBy { get; set; }

    public Supplier Supplier { get; set; } = null!;
    public User? Creator { get; set; }
    public ICollection<InboundItem> Items { get; set; } = [];
}
