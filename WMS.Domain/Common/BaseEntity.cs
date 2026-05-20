using WMS.Domain.Interfaces;

namespace WMS.Domain.Common;

//public abstract class BaseEntity : IBaseEntity<Guid>
//{
//    public Guid Id { get; set; } = Guid.NewGuid();
//    public DateTime CreatedAt { get; set; }
//    public Guid? CreatedBy { get; set; }
//    public DateTime UpdatedAt { get; set; }
//    public Guid? UpdatedBy { get; set; }
//    public bool IsDeleted { get; set; }
//    public DateTime? DeletedAt { get; set; }

//    private readonly List<DomainEvent> _domainEvents = [];
//    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

//    protected void AddDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);
//    public void ClearDomainEvents() => _domainEvents.Clear();
//    protected static string GenerateOrderNumber(string prefix)
//    {
//        return $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
//    }
//}

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted => DeletedAt.HasValue;
}
