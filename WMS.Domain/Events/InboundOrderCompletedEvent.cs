using WMS.Domain.Common;

namespace WMS.Domain.Events;

public class InboundOrderCompletedEvent(Guid orderId) : DomainEvent;
public class OutboundOrderCompletedEvent(Guid orderId) : DomainEvent;
public class StockCountApprovedEvent(Guid countId) : DomainEvent;
