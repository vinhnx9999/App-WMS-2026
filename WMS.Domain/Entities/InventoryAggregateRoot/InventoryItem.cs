using WMS.Domain.Common;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities.InventoryAggregateRoot;

public class InventoryItem : BaseEntity, IAggregateRoot
{
    private InventoryItem() { }

    private InventoryItem(
        Guid tenantId,
        Guid skuId,
        Guid locationId,
        Guid? supplierId,
        string? serialNumber,
        Guid? palletId,
        int quantity,
        decimal unitPrice,
        DateTime putawayDate,
        DateTime? expiryDate)
    {
        TenantId = tenantId;
        SkuId = skuId;
        LocationId = locationId;
        SupplierId = supplierId;
        SerialNumber = serialNumber;
        PalletId = palletId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        PutawayDate = putawayDate;
        ExpiryDate = expiryDate;
        AllocatedQuantity = 0;
        RowVersion = Guid.NewGuid().ToByteArray();
        UpdateStatus();
    }

    public static InventoryItem Create(
        Guid tenantId,
        Guid skuId,
        Guid locationId,
        Guid? supplierId,
        string? serialNumber,
        Guid? palletId,
        int quantity,
        decimal unitPrice,
        DateTime putawayDate,
        DateTime? expiryDate)
    {
        return new InventoryItem(
            tenantId,
            skuId,
            locationId,
            supplierId,
            serialNumber,
            palletId,
            quantity,
            unitPrice,
            putawayDate,
            expiryDate);
    }

    public Guid SkuId { get; private set; }
    public Guid LocationId { get; private set; }
    public Guid? SupplierId { get; private set; }
    public string? SerialNumber { get; private set; }
    public Guid? PalletId { get; private set; }
    public int Quantity { get; private set; }
    public int AllocatedQuantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public ItemStatus Status { get; private set; } = ItemStatus.Available;
    public DateTime PutawayDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }

    /// <summary>
    /// Row version
    /// </summary>
    //skipcq: CS-W1096
    public byte[] RowVersion { get; private set; } = default!;
    //skipcq: CS-W1096

    public int AvailableQuantity => Status == ItemStatus.Available ? Math.Max(0, Quantity - AllocatedQuantity) : 0;

    public void UpdateStatus()
    {
        if (Quantity <= 0)
        {
            Status = ItemStatus.OutOfStock;
        }
        else if (Status == ItemStatus.OutOfStock)
        {
            Status = ItemStatus.Available;
        }
    }

    public void Update(
        Guid skuId,
        Guid locationId,
        Guid? supplierId,
        string? serialNumber,
        Guid? palletId,
        int quantity,
        decimal unitPrice,
        DateTime putawayDate,
        DateTime? expiryDate)
    {
        SkuId = skuId;
        LocationId = locationId;
        SupplierId = supplierId;
        SerialNumber = serialNumber;
        PalletId = palletId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        PutawayDate = putawayDate;
        ExpiryDate = expiryDate;
        UpdateStatus();
        RowVersion = Guid.NewGuid().ToByteArray();
    }

    public void Hold()
    {
        if (Status != ItemStatus.OutOfStock)
        {
            Status = ItemStatus.Hold;
            RowVersion = Guid.NewGuid().ToByteArray();
        }
    }

    public void ReleaseHold()
    {
        if (Status == ItemStatus.Hold)
        {
            Status = ItemStatus.Available;
            UpdateStatus();
            RowVersion = Guid.NewGuid().ToByteArray();
        }
    }

    public void Allocate(int qty)
    {
        if (Status != ItemStatus.Available)
        {
            throw new DomainException("STOCK_NOT_AVAILABLE", "Tồn kho không khả dụng để đặt hàng.");
        }

        if (qty > AvailableQuantity)
        {
            throw new DomainException("INSUFFICIENT_STOCK", "Không đủ tồn kho khả dụng để đặt hàng.");
        }

        AllocatedQuantity += qty;
        RowVersion = Guid.NewGuid().ToByteArray();
    }

    public void ReleaseAllocation(int qty)
    {
        if (qty > AllocatedQuantity)
        {
            throw new DomainException("INVALID_RELEASE", "Số lượng giải phóng vượt quá số lượng đã đặt trước.");
        }

        AllocatedQuantity -= qty;
        RowVersion = Guid.NewGuid().ToByteArray();
    }

    public void DeductShippedStock(int pickedQuantity, int originalQuantity)
    {
        if (pickedQuantity > Quantity)
        {
            throw new DomainException("INVALID_DEDUCTION", "Số lượng thực xuất vượt quá tồn kho thực tế.");
        }

        if (originalQuantity > AllocatedQuantity)
        {
            throw new DomainException("INVALID_DEDUCTION", "Số lượng giải phóng đặt trước vượt quá số lượng đã đặt trước.");
        }

        Quantity -= pickedQuantity;
        AllocatedQuantity -= originalQuantity;
        UpdateStatus();
        RowVersion = Guid.NewGuid().ToByteArray();
    }

    public void AddStock(int qty)
    {
        if (qty < 0)
        {
            throw new DomainException("INVALID_QUANTITY", "Số lượng nhập thêm không hợp lệ.");
        }

        Quantity += qty;
        UpdateStatus();
        RowVersion = Guid.NewGuid().ToByteArray();
    }

    public void UpdateExpiryDate(DateTime? expiryDate)
    {
        ExpiryDate = expiryDate;
        RowVersion = Guid.NewGuid().ToByteArray();
    }
}
