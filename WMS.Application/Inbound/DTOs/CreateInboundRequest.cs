namespace WMS.Application.Inbound.DTOs;

public record CreateInboundRequest(
    Guid SupplierId, DateOnly? ExpectedDate, string? Notes,
    List<CreateInboundItemRequest> Items);
