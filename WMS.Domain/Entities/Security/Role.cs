using WMS.Domain.Common;

namespace WMS.Domain.Entities.Security;

public class Role : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Dictionary<string, bool> Permissions { get; set; } = [];
    public ICollection<User> Users { get; set; } = [];
}
