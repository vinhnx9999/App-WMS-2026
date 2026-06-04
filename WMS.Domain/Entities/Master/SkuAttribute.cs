using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class SkuAttribute : BaseEntity
{
    public string Code { get; set; } = null!;
    public string? Name { get; set; }
    public ICollection<SkuAttributeValue> Values { get; set; } = [];

    public static SkuAttribute Create(Guid tenantId, string code, string? name = null)
    {
        return new SkuAttribute
        {
            TenantId = tenantId,
            Code = code.Trim(),
            Name = string.IsNullOrWhiteSpace(name) ? code.Trim() : name.Trim()
        };
    }
}
