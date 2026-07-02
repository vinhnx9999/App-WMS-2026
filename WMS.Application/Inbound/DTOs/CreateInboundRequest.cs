namespace WMS.Application.Inbound.DTOs;

public record CreateInboundRequest(
    DateOnly? ExpectedDate, string? Notes,
    List<CreateInboundItemRequest> Items);
