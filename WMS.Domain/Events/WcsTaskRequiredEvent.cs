using WMS.Domain.Common;

namespace WMS.Domain.Events;

public sealed record WcsMovementItem(
    string PalletCode,
    string ToLocationCode,
    string WcsBlockId,
    Guid LocationId);

public sealed class WcsTaskRequiredEvent(
    Guid tenantId,
    Guid warehouseId,
    Guid putawayTaskId,
    IReadOnlyList<WcsMovementItem> items) : DomainEvent
{
    public Guid TenantId { get; } = tenantId;
    public Guid WarehouseId { get; } = warehouseId;
    public Guid PutawayTaskId { get; } = putawayTaskId;
    public IReadOnlyList<WcsMovementItem> Items { get; } = items;
}
