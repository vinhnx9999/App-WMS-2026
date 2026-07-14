using WMS.Domain.Enums;

namespace WMS.Application.Inbound.DTOs;

public record InboundOrderDto(
    Guid Id, string OrderNumber,
    DateOnly? ExpectedDate, InboundStatus Status,
    decimal TotalValue, int ItemsCount);
