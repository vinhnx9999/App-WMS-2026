using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class UnitOfMeasure : BaseEntity
{
    public string Code { get; set; } = null!;
    public string? Name { get; set; }
    public ICollection<SkuUnitOfMeasure> SkuUnitOfMeasures { get; set; } = [];
}
