using WMS.Domain.Common;
using WMS.Domain.Extensions;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities.Master;

public class Supplier : BaseEntity, IAggregateRoot
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Contact { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }

    private Supplier()
    {
        // For EF Core
    }

    public static Supplier Create(
        Guid tenantId,
        string code,
        string name,
        string? contact = null,
        string? phone = null,
        string? email = null,
        string? address = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("INVALID_TENANT_ID", "Tenant ID is required.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("INVALID_SUPPLIER_CODE", "Supplier code is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("INVALID_SUPPLIER_NAME", "Supplier name is required.");
        }

        return new Supplier
        {
            TenantId = tenantId,
            Code = Utilities.NormalizeCode(code),
            Name = name.Trim(),
            Contact = Utilities.NormalizeNullable(contact),
            Phone = Utilities.NormalizeNullable(phone),
            Email = Utilities.NormalizeNullable(email),
            Address = Utilities.NormalizeNullable(address)
        };
    }

    public void Update(
        string name,
        string? contact = null,
        string? phone = null,
        string? email = null,
        string? address = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("INVALID_SUPPLIER_NAME", "Supplier name is required.");
        }

        Name = name.Trim();
        Contact = Utilities.NormalizeNullable(contact);
        Phone = Utilities.NormalizeNullable(phone);
        Email = Utilities.NormalizeNullable(email);
        Address = Utilities.NormalizeNullable(address);
    }

    public void Delete(string? deletedBy = null)
    {
        MarkDeleted(deletedBy);
    }

    public void Restore(string? restoredBy = null)
    {
        if (!IsDeleted)
        {
            throw new DomainException("SUPPLIER_NOT_DELETED", "Only deleted suppliers can be restored.");
        }
        MarkRestored(restoredBy);
    }
}
