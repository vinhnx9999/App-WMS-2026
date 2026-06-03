using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class SkuAttributeValue : BaseEntity
{

    private SkuAttributeValue() { }

    public Guid SkuId { get; private set; }

    public Guid AttributeId { get; private set; }

    public string Value { get; private set; } = null!;

    internal SkuAttributeValue(
       Guid tenantId,
       Guid skuId,
       Guid attributeId,
       string value)
    {
        TenantId = tenantId;
        SkuId = skuId;
        AttributeId = attributeId;
        Value = value.Trim();
    }
}
