using WMS.Domain.Common;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities;

public class UnitOfMeasure : BaseEntity, IAggregateRoot
{
    private UnitOfMeasure() { }
    public string Code { get; private set; } = null!;
    public string? Name { get; private set; }
    public string? Description { get; private set; }

    public static UnitOfMeasure Create(
        Guid tenantId,
        string code,
        string? name,
        string? description)
    {
        return new UnitOfMeasure
        {
            TenantId = tenantId,
            Code = code,
            Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        };
    }

    public void Rename(string? name, string? description)
    {
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void Delete(string? deletedBy)
    {
        MarkDeleted(deletedBy);
    }
}
