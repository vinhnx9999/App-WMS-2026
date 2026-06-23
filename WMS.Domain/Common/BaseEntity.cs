namespace WMS.Domain.Common;

public abstract class BaseEntity
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>
    /// Tenant id
    /// </summary>
    public Guid TenantId { get; protected set; } = Guid.NewGuid();

    /// <summary>
    /// Created At
    /// </summary> 
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// Created By
    /// </summary>
    public string? CreatedBy { get; protected set; }

    /// <summary>
    /// Updated At
    /// </summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>
    /// Updated By
    /// </summary>
    public string? UpdatedBy { get; protected set; }

    /// <summary>
    /// Deleted At
    /// </summary>
    public DateTime? DeletedAt { get; protected set; }

    /// <summary>
    /// Deleted By
    /// </summary>
    public string? DeletedBy { get; protected set; }

    /// <summary>
    /// Is Deleted
    /// </summary>
    public bool IsDeleted { get; private set; }

    public void MarkDeleted(string? deletedBy = null)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void MarkRestored()
    {
        if (!IsDeleted)
        {
            return;
        }

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }


    private readonly List<DomainEvent> _domainEvents = [];

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearEvents()
    {
        _domainEvents.Clear();
    }
}
