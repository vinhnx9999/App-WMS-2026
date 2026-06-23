using WMS.Domain.Common;
using WMS.Domain.Extensions;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities.Master;

public class Customer : BaseEntity, IAggregateRoot
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public string? Type { get; private set; }

    private Customer()
    {
        // For EF Core
    }

    public static Customer Create(
        Guid tenantId,
        string code,
        string name,
        string? address = null,
        string? phone = null,
        string? type = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("INVALID_TENANT_ID", "Tenant ID is required.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("INVALID_CUSTOMER_CODE", "Customer code is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("INVALID_CUSTOMER_NAME", "Customer name is required.");
        }

        return new Customer
        {
            TenantId = tenantId,
            Code = Utilities.NormalizeCode(code),
            Name = name.Trim(),
            Address = Utilities.NormalizeNullable(address),
            Phone = Utilities.NormalizeNullable(phone),
            Type = Utilities.NormalizeNullable(type)
        };
    }

    public void Update(
        string name,
        string? address = null,
        string? phone = null,
        string? type = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("INVALID_CUSTOMER_NAME", "Customer name is required.");
        }

        Name = name.Trim();
        Address = Utilities.NormalizeNullable(address);
        Phone = Utilities.NormalizeNullable(phone);
        Type = Utilities.NormalizeNullable(type);
    }

    public void Delete(string? deletedBy = null)
    {
        MarkDeleted(deletedBy);
    }

    public void Restore()
    {
        if (!IsDeleted)
        {
            throw new DomainException("CUSTOMER_NOT_DELETED", "Only deleted customers can be restored.");
        }
        MarkRestored();
    }
}