namespace WMS.Application.Inbound.DTOs;

public record ReceiveInboundRequest(
    List<ReceiveItemRequest> Items);
