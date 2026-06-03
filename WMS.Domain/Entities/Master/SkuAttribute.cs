using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class SkuAttribute : BaseEntity
{
    public string Code { get; set; } = null!;
    public string? Name { get; set; }
    public ICollection<SkuAttributeValue> Values { get; set; } = [];
}
