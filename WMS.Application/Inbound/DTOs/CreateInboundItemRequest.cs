namespace WMS.Application.Inbound.DTOs;

public record CreateInboundItemRequest(Guid SkuId, int Quantity, Guid? SupplierId = null);
