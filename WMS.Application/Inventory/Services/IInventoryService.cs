using WMS.Application.Common.Models;
using WMS.Application.Inventory.DTOs;

namespace WMS.Application.Inventory.Services;

public interface IInventoryService
{
    Task<PagedResult<InventoryDto>> GetListAsync(InventoryQuery query, CancellationToken ct = default);
    Task<InventoryDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<InventoryDto> CreateAsync(CreateInventoryRequest request, CancellationToken ct = default);
    Task UpdateAsync(Guid id, UpdateInventoryRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
