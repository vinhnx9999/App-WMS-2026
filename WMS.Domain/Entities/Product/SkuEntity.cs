using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class SkuEntity : BaseEntity
{
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public string SkuCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal? Price { get; set; }

    // Navigation
    public ICollection<InventoryItem> InventoryItems { get; set; } = [];
}
