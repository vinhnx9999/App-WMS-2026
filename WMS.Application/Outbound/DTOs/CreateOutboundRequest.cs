namespace WMS.Application.Outbound.DTOs;

public record CreateOutboundRequest(
    Guid PartnerId, string? Destination, DateOnly? ExpectedDelivery,
    string? Notes, List<CreateOutboundItemRequest> Items);
