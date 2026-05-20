using WMS.Application.Outbound.DTOs;

namespace WMS.Application.Outbound.Services;

public interface IOutboundService
{
    Task<List<OutboundOrderDto>> GetListAsync(CancellationToken ct = default);
    Task<OutboundOrderDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<OutboundOrderDto> CreateAsync(CreateOutboundRequest request, CancellationToken ct = default);
    Task ShipAsync(Guid orderId, ShipOutboundRequest request, CancellationToken ct = default);
    Task CancelAsync(Guid orderId, CancellationToken ct = default);
}
