using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.Warehouses;
using WMS.Domain.Interfaces;

namespace WMS.Application.Warehouse.Services;

public class WarehouseProvisioningService(IUnitOfWork uow)
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<(WarehouseArea DefaultArea, Block DefaultBlock)> EnsureDefaultStructureAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        var warehouse = await _uow.Repository<WMS.Domain.Entities.Warehouses.Warehouse>().Query()
            .Include(w => w.Areas)
            .ThenInclude(a => a.Blocks)
            .FirstOrDefaultAsync(w => w.Id == warehouseId && !w.IsDeleted, cancellationToken);

        if (warehouse == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "NOT_FOUND", "Warehouse Not Found");
        }

        var result = warehouse.EnsureDefaultStructure();

        await _uow.SaveChangesAsync(cancellationToken);
        return result;
    }
}
