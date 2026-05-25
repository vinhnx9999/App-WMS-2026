using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class SkuSpecification : BaseEntity
{
    public Guid SkuId { get; set; }
    public SkuEntity Sku { get; set; } = null!;
    public Guid SpecificationId { get; set; }
    public Specification Specification { get; set; } = null!;
}
