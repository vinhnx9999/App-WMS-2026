using WMS.Domain.Common;
using WMS.Domain.Entities.Master;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities.Outbound;

public class OutboundOrder : BaseEntity, IAggregateRoot
{
    public string ShipmentNumber { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public string? Destination { get; private set; }
    public DateOnly? ExpectedDelivery { get; private set; }
    public OutboundStatus Status { get; private set; } = OutboundStatus.Pending;
    public decimal TotalValue { get; private set; }

    public Customer Customer { get; private set; } = null!;
    public ICollection<OutboundItem> Items { get; private set; } = [];
    public string? Notes { get; private set; }

    private OutboundOrder()
    {
        // For EF Core
    }

    public void Ship()
    {
        if (Status == OutboundStatus.Shipped || Status == OutboundStatus.Delivered)
        {
            throw new DomainException("ALREADY_SHIPPED", "Đơn hàng đã được xuất kho trước đó.");
        }

        if (Status == OutboundStatus.Cancelled)
        {
            throw new DomainException("CANNOT_SHIP_CANCELLED", "Không thể xuất kho đơn hàng đã bị hủy.");
        }

        Status = OutboundStatus.Shipped;
    }

    public void Cancel()
    {
        if (Status == OutboundStatus.Shipped || Status == OutboundStatus.Delivered)
        {
            throw new DomainException("CANNOT_CANCEL_SHIPPED", "Không thể hủy đơn hàng đã xuất kho.");
        }

        Status = OutboundStatus.Cancelled;
    }

    public void UpdateTotalValue(decimal totalValue)
    {
        if (totalValue < 0)
        {
            throw new DomainException("INVALID_TOTAL_VALUE", "Tổng giá trị đơn xuất không được phép âm.");
        }

        TotalValue = totalValue;
    }


    public static OutboundOrder Create(
        Guid tenantId,
        string shipmentNumber,
        Guid customerId,
        string? destination,
        DateOnly? expectedDelivery,
        string? notes,
        IEnumerable<OutboundItem>? items = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(shipmentNumber))
        {
            throw new ArgumentException("ShipmentNumber is required.", nameof(shipmentNumber));
        }

        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("CustomerId is required.", nameof(customerId));
        }

        var order = new OutboundOrder
        {
            TenantId = tenantId,
            ShipmentNumber = shipmentNumber.Trim(),
            CustomerId = customerId,
            Destination = destination?.Trim(),
            ExpectedDelivery = expectedDelivery,
            Notes = notes?.Trim(),
            Status = OutboundStatus.Pending,
            TotalValue = 0
        };

        if (items != null)
        {
            foreach (var item in items)
            {
                order.Items.Add(item);
            }
        }

        return order;
    }
}
