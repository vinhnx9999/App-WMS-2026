using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WMS.Application.Warehouse.Services;
using WMS.Domain.Common;
using WMS.Domain.Entities.Warehouses;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Warehouses;

public class WarehouseTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        return connection;
    }

    private static WmsDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(connection)
            .Options;

        return new WmsDbContext(options, Mock.Of<ICurrentUser>());
    }

    private static UnitOfWork CreateUnitOfWork(WmsDbContext db)
    {
        return new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
    }

    [Fact]
    public async Task Warehouse_EnsureDefaultStructure_ShouldProvisionDefaultAreaAndBlock()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var warehouse = new Warehouse(TenantId, "Main WH", "WH01", "123 Street");
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (area, block) = warehouse.EnsureDefaultStructure();
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        area.Should().NotBeNull();
        area.IsDefault.Should().BeTrue();
        area.Code.Should().Be("DEFAULT");

        block.Should().NotBeNull();
        block.IsDefault.Should().BeTrue();
        block.Code.Should().Be("DEFAULT");

        warehouse.Areas.Should().ContainSingle();
        warehouse.Areas.First().Blocks.Should().ContainSingle();

        // Ensure calling again does not duplicate
        var (area2, block2) = warehouse.EnsureDefaultStructure();
        area2.Id.Should().Be(area.Id);
        block2.Id.Should().Be(block.Id);
        warehouse.Areas.Should().ContainSingle();
    }

    [Fact]
    public void WarehouseRuleSetting_Create_MultipleSpatialTiers_ShouldThrowDomainException()
    {
        // Arrange
        var whId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();

        // Act & Assert
        Action act = () => WarehouseRuleSetting.Create(
            tenantId: TenantId,
            warehouseId: whId,
            locationId: null,
            zoneId: zoneId,
            blockId: null,
            areaId: areaId,
            skuId: null,
            supplierId: null,
            ruleType: WarehouseRuleType.FIFO);

        act.Should().Throw<DomainException>()
            .And.Code.Should().Be("INVALID_RULE_SCOPE");
    }

    [Fact]
    public void WarehouseRuleSetting_Create_TargetingDefaultArea_ShouldThrowDomainException()
    {
        // Arrange
        var whId = Guid.NewGuid();
        var areaId = Guid.NewGuid();

        // Act & Assert
        Action act = () => WarehouseRuleSetting.Create(
            tenantId: TenantId,
            warehouseId: whId,
            locationId: null,
            zoneId: null,
            blockId: null,
            areaId: areaId,
            skuId: null,
            supplierId: null,
            ruleType: WarehouseRuleType.FIFO,
            isAreaDefault: true);

        act.Should().Throw<DomainException>()
            .And.Code.Should().Be("INVALID_RULE_TARGET");
    }

    [Fact]
    public async Task WarehouseRuleResolutionService_ShouldResolveHighestSpecificityRule()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Setup base data
        var warehouse = new Warehouse(TenantId, "Main WH", "WH01", "123 Street");
        var (defaultArea, defaultBlock) = warehouse.EnsureDefaultStructure();
        db.Warehouses.Add(warehouse);

        // Create a non-default area and block
        var customArea = new WarehouseArea(TenantId, warehouse.Id, "Custom Area", "CUSTOM_AREA");
        warehouse.Areas.Add(customArea);

        var customBlock = new Block(TenantId, warehouse.Id, customArea.Id, "Custom Block", "CUSTOM_BLOCK");
        customArea.Blocks.Add(customBlock);

        // Create zone and location
        var zone = new Zone(TenantId, "Special Zone", "ZONE01", ZoneType.Standard);
        db.Zones.Add(zone);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var location = new LocationEntity(
            tenantId: TenantId,
            warehouseId: warehouse.Id,
            areaId: customArea.Id,
            blockId: customBlock.Id,
            zoneId: zone.Id,
            name: "LOC-01",
            x: 1,
            y: 2,
            z: 3
        );
        db.Locations.Add(location);

        // Add rules with varying specificity
        var skuId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        // Rule 1: Area-level rule (Score: AreaId(+2) = 2) -> Strategy: LIFO
        var rule1 = WarehouseRuleSetting.Create(
            tenantId: TenantId,
            warehouseId: warehouse.Id,
            locationId: null,
            zoneId: null,
            blockId: null,
            areaId: customArea.Id,
            skuId: null,
            supplierId: null,
            ruleType: WarehouseRuleType.LIFO);
        db.WarehouseRuleSettings.Add(rule1);

        // Rule 2: Zone-level + Sku-level rule (Score: ZoneId(+8) + SkuId(+2) = 10) -> Strategy: FEFO
        var rule2 = WarehouseRuleSetting.Create(
            tenantId: TenantId,
            warehouseId: warehouse.Id,
            locationId: null,
            zoneId: zone.Id,
            blockId: null,
            areaId: null,
            skuId: skuId,
            supplierId: null,
            ruleType: WarehouseRuleType.FEFO);
        db.WarehouseRuleSettings.Add(rule2);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var uow = CreateUnitOfWork(db);
        var resolutionService = new WarehouseRuleResolutionService(uow);

        // Act
        // Resolving for the custom location, Sku, Supplier.
        // Applicable rules:
        // - Rule 1 (Area matches customArea.Id). Score = 2. Strategy = LIFO.
        // - Rule 2 (Zone matches zone.Id, Sku matches skuId). Score = 8 + 2 = 10. Strategy = FEFO.
        // Rule 2 has higher score (10 > 2), so FEFO should win.
        var strategy = await resolutionService.ResolvePickingStrategyAsync(
            warehouse.Id, location.Id, skuId, supplierId, TestContext.Current.CancellationToken);

        // Assert
        strategy.Should().Be(WarehouseRuleType.FEFO);
    }
}
