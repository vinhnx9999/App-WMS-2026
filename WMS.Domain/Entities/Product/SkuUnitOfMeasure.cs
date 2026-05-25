using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class SkuUnitOfMeasure : BaseEntity
{
    public Guid SkuId { get; set; }
    public SkuEntity Sku { get; set; } = null!;
    public Guid UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;
    public decimal ConversionFactor { get; set; }
}
