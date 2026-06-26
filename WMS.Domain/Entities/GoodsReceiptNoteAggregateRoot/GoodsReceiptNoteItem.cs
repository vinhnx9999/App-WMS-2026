using WMS.Domain.Common;

namespace WMS.Domain.Entities.GoodsReceiptNoteAggregateRoot;

public class GoodsReceiptNoteItem : BaseEntity
{
    public Guid SkuId { get; private set; }
    public int Quantity { get; private set; }
    public Guid LocationId { get; private set; }
    public Guid InventoryItemId { get; private set; }

    public GoodsReceiptNoteItem(Guid skuId, int quantity, Guid locationId, Guid inventoryItemId)
    {
        SkuId = skuId;
        Quantity = quantity;
        LocationId = locationId;
        InventoryItemId = inventoryItemId;
    }
}
