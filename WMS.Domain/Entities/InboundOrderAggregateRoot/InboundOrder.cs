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
    public string OrderNumber { get; private set; } = null!;
    public DateOnly? ExpectedDate { get; private set; }
    public DateOnly? ReceivedDate { get; private set; }
    public InboundStatus Status { get; private set; } = InboundStatus.Pending;
    public decimal TotalValue { get; private set; }

    /// <summary>
    /// Notes
    /// </summary>
    // TODO : Need more description for this PO 
    // Should Use value objects
    public string? Notes { get; private set; }

    private readonly List<InboundItem> _items = new();
    public virtual IReadOnlyCollection<InboundItem> Items => _items.AsReadOnly();

    private InboundOrder() { }

    private InboundOrder(
        Guid tenantId,
        string orderNumber,
        DateOnly? expectedDate,
        string? notes)
    {
        TenantId = tenantId;
        OrderNumber = orderNumber;
        ExpectedDate = expectedDate;
        Notes = notes;
        Status = InboundStatus.Pending;
        TotalValue = 0;
    }

    public static InboundOrder Create(
        Guid tenantId,
        string orderNumber,
        DateOnly? expectedDate,
        string? notes)
    {
        return new InboundOrder(tenantId, orderNumber, expectedDate, notes);
    }

    public void AddItem(Guid skuId, int quantity, Guid? supplierId, DateOnly? expiryDate = null, string? serialNumber = null, string? lotNumber = null)
    {
        if (Status == InboundStatus.Completed)
        {
            throw new DomainException("ORDER_ALREADY_COMPLETED", "Đơn nhập đã hoàn tất, không thể thêm mặt hàng.");
        }

        var existingItem = _items.FirstOrDefault(i => i.SkuId == skuId);
        if (existingItem != null)
        {
            existingItem.AddQuantity(quantity);
        }
        else
        {
            _items.Add(InboundItem.Create(Id, skuId, quantity, supplierId, expiryDate, serialNumber, lotNumber));
        }
    }

    public void SetTotalValue(decimal totalValue)
    {
        TotalValue = totalValue;
    }

    public void ReceiveItem(Guid skuId, int receivedQuantity, string? note)
    {
        if (Status == InboundStatus.Completed)
        {
            throw new DomainException("ORDER_ALREADY_COMPLETED", "Đơn nhập đã hoàn tất.");
        }

        var item = _items.FirstOrDefault(x => x.SkuId == skuId);
        if (item != null)
        {
            item.Receive(receivedQuantity, note);
        }
    }

    public void CompleteOrder()
    {
        if (Status == InboundStatus.Completed)
        {
            return;
        }

        Status = InboundStatus.Completed;
        ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public void Cancel()
    {
        if (Status == InboundStatus.Completed)
        {
            throw new DomainException("ORDER_ALREADY_COMPLETED", "Không thể hủy đơn nhập đã hoàn thành.");
        }

        Status = InboundStatus.Cancelled;
    }
}
