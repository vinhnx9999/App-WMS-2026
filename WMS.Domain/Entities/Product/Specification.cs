using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Specification : BaseEntity
{
    public string Code { get; set; } = null!;
    public string? Name { get; set; }
    public ICollection<SkuSpecification> SkuSpecifications { get; set; } = [];
}
