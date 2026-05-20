using WMS.Domain.Enums;

namespace WMS.Application.Inbound.DTOs;

public record InboundOrderDto(
    Guid Id, string OrderNumber, string SupplierName,
    DateOnly? ExpectedDate, InboundStatus Status,
    decimal TotalValue, int ItemsCount,
    List<InboundItemDto> Items);
