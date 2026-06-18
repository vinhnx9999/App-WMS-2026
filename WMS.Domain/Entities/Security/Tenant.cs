using WMS.Domain.Common;

namespace WMS.Domain.Entities.Security;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string TenantInfo { get; set; } = null!;
    public string CompanyName { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string TaxNumber { get; set; } = null!;
}
