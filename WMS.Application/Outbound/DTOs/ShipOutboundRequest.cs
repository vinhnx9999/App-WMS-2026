namespace WMS.Application.Outbound.DTOs;

public record ShipOutboundRequest(List<ShipItemRequest> Items);
