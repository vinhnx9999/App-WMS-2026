using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Slug { get; set; }
    public string? Description { get; set; }

    public static Category Create(Guid tenantId, string name, string? description = null)
    {
        return new Category
        {
            TenantId = tenantId,
            Name = name.Trim(),
            Description = description?.Trim()
        };
    }

    public void Update(string name, string? description)
    {
        Name = name.Trim();
        Description = description?.Trim();
    }
}
