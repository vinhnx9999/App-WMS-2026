using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Slug { get; set; }
    public ICollection<InventoryItem> Items { get; set; } = [];
}
