using WMS.Application.Inbound.DTOs;

namespace WMS.Application.Inbound.Services;

public interface IInboundService
{
    Task CancelAsync(Guid id, CancellationToken ct);
    Task<InboundOrderDto?> CreateAsync(CreateInboundRequest request, CancellationToken ct);
    Task ReceiveAsync(Guid id, ReceiveInboundRequest request, CancellationToken ct);
}
