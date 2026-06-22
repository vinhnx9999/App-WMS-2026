using WMS.Domain.Common;
using WMS.Domain.Entities.Inbound;
using WMS.Domain.Entities.Outbound;
using WMS.Domain.Entities.Warehouses;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class InventoryItem : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public Guid? SkuId { get; set; }
    public Guid? LocationId { get; set; }
    public int Quantity { get; set; }
    public int MinQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public ItemStatus Status { get; set; } = ItemStatus.InStock;
    public string? Barcode { get; set; }

    // Navigation    
    public LocationEntity? Location { get; set; }
    public Product.Sku? Sku { get; set; }
    public ICollection<InboundItem> InboundItems { get; set; } = [];
    public ICollection<OutboundItem> OutboundItems { get; set; } = [];
    public string? SkuCode { get; set; }
    public string? CategoryName { get; set; }
    public Guid? CategoryId { get; set; }
    public string? ZoneName { get; set; }
    public Guid? ZoneId { get; set; }
    public string? LocationName { get; set; }

    // Methods
    public void UpdateStatus()
    {
        Status = Quantity <= 0 ? ItemStatus.OutOfStock
               : Quantity <= MinQuantity ? ItemStatus.LowStock
               : ItemStatus.InStock;
    }
}
