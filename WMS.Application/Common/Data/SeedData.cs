using WMS.Infrastructure.Persistence;

namespace WMS.Application.Common.Data;

public static class SeedData
{
    public static async Task InitializeAsync(WmsDbContext db)
    {
        //if (db.Set<Role>().Any()) return;

        //var adminRole = new Role
        //{
        //    Name = "admin",
        //    Description = "Quản trị",
        //    Permissions = new Dictionary<string, bool> { ["full"] = true }
        //};
        //var managerRole = new Role { Name = "manager", Description = "Quản lý kho" };
        //var keeperRole = new Role { Name = "keeper", Description = "Thủ kho" };
        //var plannerRole = new Role { Name = "planner", Description = "Kế hoạch" };
        //var viewerRole = new Role { Name = "viewer", Description = "Chỉ xem" };
        //db.Set<Role>().AddRange(adminRole, managerRole, keeperRole, plannerRole, viewerRole);

        //db.Set<User>().Add(new User
        //{
        //    Email = "admin@wms.vn",
        //    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123@"),
        //    FullName = "Nguyễn Văn Admin",
        //    RoleId = adminRole.Id,
        //});

        //var catPhone = new Category { Name = "Điện thoại", Slug = "dien-thoai" };
        //var catLaptop = new Category { Name = "Laptop", Slug = "laptop" };
        //var catAcc = new Category { Name = "Phụ kiện", Slug = "phu-kien" };
        //db.Categories.AddRange(catPhone, catLaptop, catAcc);

        //var zoneA = new Zone { Name = "Khu A", ZoneCode = "ZA", TotalLocations = 480 };
        //var zoneB = new Zone { Name = "Khu B", ZoneCode = "ZB", TotalLocations = 720 };
        //var zoneC = new Zone { Name = "Khu C", ZoneCode = "ZC", TotalLocations = 360 };
        //db.Zones.AddRange(zoneA, zoneB, zoneC);

        //var tenantId = Guid.Empty;
        //db.Suppliers.AddRange(
        //    Supplier.Create(tenantId, "SAM-VINA", "Samsung Vina", email: "sales@samsung.vn"),
        //    Supplier.Create(tenantId, "APL-VN", "Apple Vietnam", email: "supply@apple.vn"));

        //db.Partners.AddRange(
        //    new Partner { Name = "FPT Shop", Type = "Retail" },
        //    new Partner { Name = "Thế Giới Di Động", Type = "Retail" });

        //db.InventoryItems.AddRange(
        //    new InventoryItem
        //    {
        //        Sku = "SAM-S24U",
        //        Name = "Samsung Galaxy S24 Ultra",
        //        CategoryId = catPhone.Id,
        //        ZoneId = zoneA.Id,
        //        Location = "A1-03",
        //        Quantity = 342,
        //        MinQuantity = 50,
        //        UnitPrice = 29990000
        //    },
        //    new InventoryItem
        //    {
        //        Sku = "APL-IP15P",
        //        Name = "iPhone 15 Pro Max",
        //        CategoryId = catPhone.Id,
        //        ZoneId = zoneA.Id,
        //        Location = "A2-01",
        //        Quantity = 189,
        //        MinQuantity = 30,
        //        UnitPrice = 34990000
        //    },
        //    new InventoryItem
        //    {
        //        Sku = "SNY-WH1000",
        //        Name = "Sony WH-1000XM5",
        //        CategoryId = catAcc.Id,
        //        ZoneId = zoneA.Id,
        //        Location = "A3-05",
        //        Quantity = 3,
        //        MinQuantity = 50,
        //        UnitPrice = 7990000
        //    },
        //    new InventoryItem
        //    {
        //        Sku = "DLX-XPS15",
        //        Name = "Dell XPS 15",
        //        CategoryId = catLaptop.Id,
        //        ZoneId = zoneC.Id,
        //        Location = "C1-01",
        //        Quantity = 87,
        //        MinQuantity = 20,
        //        UnitPrice = 35990000
        //    },
        //    new InventoryItem
        //    {
        //        Sku = "ANK-USBC",
        //        Name = "Cáp USB-C 60W Anker",
        //        CategoryId = catAcc.Id,
        //        ZoneId = zoneB.Id,
        //        Location = "B2-01",
        //        Quantity = 45,
        //        MinQuantity = 80,
        //        UnitPrice = 299000
        //    });

        // foreach (var item in db.InventoryItems.Local) item.UpdateStatus();

        // warehouse Seeding 
        //if (!db.Warehouses.Any())
        //{
        // var tenantId = Guid.NewGuid();
        //    var wh1 = new Domain.Entities.Warehouses.Warehouse(tenantId, "Kho Chính", "KHO_CHINH", "Hà Nội");
        //    wh1.EnsureDefaultStructure();

        //    var wh2 = new Domain.Entities.Warehouses.Warehouse(tenantId, "Kho Phụ", "KHO_PHU", "TP. Hồ Chí Minh");
        //    wh2.EnsureDefaultStructure();

        //    db.Warehouses.AddRange(wh1, wh2);
        //}

        await db.SaveChangesAsync();
    }
}