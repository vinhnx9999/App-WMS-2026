using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class SkuEntity : BaseEntity
{
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public string SkuCode { get; set; } = null!;
    public string? Name { get; set; }
    public string? GoodsNature { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }

    public ICollection<InventoryItem> InventoryItems { get; set; } = [];
    public ICollection<SkuSpecification> SkuSpecifications { get; set; } = [];
    public ICollection<SkuUnitOfMeasure> SkuUnitOfMeasures { get; set; } = [];
}
