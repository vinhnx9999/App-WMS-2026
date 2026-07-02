namespace WMS.Application.Common.Data;

public static class WarehouseMapSeeder
{
    //public static async Task SeedAsync(WmsDbContext db)
    //{
    //    // 1. Ensure at least one warehouse exists
    //    var warehouse = await db.Warehouses.Include(w => w.Areas).ThenInclude(a => a.Blocks).FirstOrDefaultAsync();
    //    //var tenantId = Guid.Empty;
    //    var tenantId = Guid.Parse("7647ce3b-ff19-4c0f-925c-be0101d718b8");

    //    if (warehouse == null)
    //    {
    //        warehouse = new WMS.Domain.Entities.WarehouseAggregateRoot.Warehouse(tenantId, "Kho Trung Tâm 3D", "KHO_3D", "Hà Nội");
    //        db.Warehouses.Add(warehouse);
    //        await db.SaveChangesAsync();
    //    }

    //    // 2. Ensure default area & block exist
    //    var (defaultArea, defaultBlock) = warehouse.EnsureDefaultStructure();

    //    // Save warehouse structure updates if any
    //    await db.SaveChangesAsync();

    //    // 3. Check if locations are already seeded
    //    if (await db.Locations.AnyAsync(l => l.WarehouseId == warehouse.Id))
    //    {
    //        return;
    //    }

    //    var locationsToSeed = new List<LocationEntity>();

    //    // We will seed a 3-floor map
    //    // Z (floors) = 1, 2, 3
    //    // X (bays / columns) = 1 to 15
    //    // Y (rows) = 1 to 10
    //    for (int z = 1; z <= 3; z++)
    //    {
    //        for (int x = 1; x <= 15; x++)
    //        {
    //            for (int y = 1; y <= 10; y++)
    //            {
    //                string name = $"{z}.{x}.{y}";
    //                var type = LocationType.STORAGE_SLOT;

    //                // Columns 3, 6, 9, 12, 15 are aisles (paths)
    //                if (x == 3 || x == 6 || x == 9 || x == 12 || x == 15)
    //                {
    //                    // Column 15, Row 5 is the elevator/lift
    //                    if (x == 15 && y == 5)
    //                    {
    //                        type = LocationType.LIFT_POINT;
    //                    }
    //                    else
    //                    {
    //                        type = LocationType.HORIZONTAL_PATH;
    //                    }
    //                }



    //                var location = LocationEntity.Create(
    //                    tenantId,
    //                    warehouse.Id,
    //                    defaultArea.Id,
    //                    defaultBlock.Id,
    //                    null,
    //                    name,
    //                    x,
    //                    y,
    //                    z
    //                );

    //                location.SetType(type);

    //                // Block a few storage slots to show "blocked" (red) status
    //                if (type == LocationType.STORAGE_SLOT)
    //                {
    //                    // Floor 1 blocked: (2,3), (5,7)
    //                    // Floor 2 blocked: (8,4), (11,2)
    //                    // Floor 3 blocked: (1,9), (14,6)
    //                    if ((z == 1 && ((x == 2 && y == 3) || (x == 5 && y == 7))) ||
    //                        (z == 2 && ((x == 8 && y == 4) || (x == 11 && y == 2))) ||
    //                        (z == 3 && ((x == 1 && y == 9) || (x == 14 && y == 6))))
    //                    {
    //                        location.SetBlocked(true);
    //                    }
    //                }

    //                locationsToSeed.Add(location);
    //            }
    //        }
    //    }

    //    await db.Locations.AddRangeAsync(locationsToSeed);
    //    await db.SaveChangesAsync();
    //}
}
