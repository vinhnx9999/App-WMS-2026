using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WMS.Application.Inbound.Queries.GetInboundWorkflowConfig;
using WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Inbound;

public sealed class GetInboundWorkflowConfigQueryHandlerTests
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

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

        return new WmsDbContext(options, Mock.Of<ICurrentUser>(), Mock.Of<MediatR.IMediator>());
    }

    private static IUnitOfWork CreateUnitOfWork(WmsDbContext db)
    {
        return new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
    }

    [Fact]
    public async Task Handle_WhenCustomConfigExistsForWarehouse_ReturnsCustomConfig()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var warehouseId = Guid.NewGuid();
        var customConfig = new InboundWorkflowConfig(TenantA, warehouseId, null, null, allowOverReceive: true, overReceiveTolerancePercentage: 15m);
        customConfig.UpdateSteps(new List<InboundStepDefinition>
        {
            new(InboundStepType.PO, 0, "Custom PO"),
            new(InboundStepType.Putaway, 1, "Custom Putaway")
        });

        db.Set<InboundWorkflowConfig>().Add(customConfig);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var uow = CreateUnitOfWork(db);
        var handler = new GetInboundWorkflowConfigQueryHandler(uow);

        // Act
        var result = await handler.Handle(
            new GetInboundWorkflowConfigQuery(TenantA, warehouseId),
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(customConfig.Id);
        result.WarehouseId.Should().Be(warehouseId);
        result.AllowOverReceive.Should().BeTrue();
        result.OverReceiveTolerancePercentage.Should().Be(15m);
        result.Steps.Should().HaveCount(2);
        result.Steps[0].StepType.Should().Be(InboundStepType.PO);
        result.Steps[0].DisplayName.Should().Be("Custom PO");
        result.Steps[1].StepType.Should().Be(InboundStepType.Putaway);
        result.Steps[1].DisplayName.Should().Be("Custom Putaway");
    }

    [Fact]
    public async Task Handle_WhenNoCustomConfigButTenantGlobalConfigExists_ReturnsTenantGlobalConfig()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var warehouseId = Guid.NewGuid();
        var globalConfig = new InboundWorkflowConfig(TenantA, null, null, null, allowOverReceive: false, overReceiveTolerancePercentage: null);
        globalConfig.UpdateSteps(new List<InboundStepDefinition>
        {
            new(InboundStepType.PO, 0, "Global PO"),
            new(InboundStepType.Receive, 1, "Global Receive"),
            new(InboundStepType.Putaway, 2, "Global Putaway")
        });

        db.Set<InboundWorkflowConfig>().Add(globalConfig);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var uow = CreateUnitOfWork(db);
        var handler = new GetInboundWorkflowConfigQueryHandler(uow);

        // Act
        var result = await handler.Handle(
            new GetInboundWorkflowConfigQuery(TenantA, warehouseId),
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(globalConfig.Id);
        result.WarehouseId.Should().BeNull();
        result.AllowOverReceive.Should().BeFalse();
        result.OverReceiveTolerancePercentage.Should().BeNull();
        result.Steps.Should().HaveCount(3);
        result.Steps[0].StepType.Should().Be(InboundStepType.PO);
        result.Steps[0].DisplayName.Should().Be("Global PO");
        result.Steps[1].StepType.Should().Be(InboundStepType.Receive);
        result.Steps[1].DisplayName.Should().Be("Global Receive");
        result.Steps[2].StepType.Should().Be(InboundStepType.Putaway);
        result.Steps[2].DisplayName.Should().Be("Global Putaway");
    }

    [Fact]
    public async Task Handle_WhenNoConfigsExistInDb_ReturnsHardcodedDefaultConfig()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var warehouseId = Guid.NewGuid();
        var uow = CreateUnitOfWork(db);
        var handler = new GetInboundWorkflowConfigQueryHandler(uow);

        // Act
        var result = await handler.Handle(
            new GetInboundWorkflowConfigQuery(TenantA, warehouseId),
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeNull();
        result.WarehouseId.Should().BeNull();
        result.AllowOverReceive.Should().BeTrue();
        result.Steps.Should().HaveCount(4);
        result.Steps[0].StepType.Should().Be(InboundStepType.PO);
        result.Steps[0].DisplayName.Should().Be("Plan/PO");
        result.Steps[1].StepType.Should().Be(InboundStepType.Receive);
        result.Steps[1].DisplayName.Should().Be("Receive");
        result.Steps[2].StepType.Should().Be(InboundStepType.QC);
        result.Steps[2].DisplayName.Should().Be("Quality Control");
        result.Steps[3].StepType.Should().Be(InboundStepType.Putaway);
        result.Steps[3].DisplayName.Should().Be("Putaway");
    }

    [Fact]
    public async Task Handle_ShouldOnlyRetrieveConfigsBelongingToRequestedTenant()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var warehouseId = Guid.NewGuid();

        // Custom config for Tenant B (should be ignored by Tenant A)
        var customConfigB = new InboundWorkflowConfig(TenantB, warehouseId, null, null, allowOverReceive: true, overReceiveTolerancePercentage: 20m);
        customConfigB.UpdateSteps(new List<InboundStepDefinition>
        {
            new(InboundStepType.PO, 0, "PO Tenant B"),
            new(InboundStepType.Putaway, 1, "Putaway Tenant B")
        });

        db.Set<InboundWorkflowConfig>().Add(customConfigB);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var uow = CreateUnitOfWork(db);
        var handler = new GetInboundWorkflowConfigQueryHandler(uow);

        // Act
        var result = await handler.Handle(
            new GetInboundWorkflowConfigQuery(TenantA, warehouseId),
            TestContext.Current.CancellationToken);

        // Assert: Since Tenant B's config is ignored, Tenant A should fall back to the default config.
        result.Should().NotBeNull();
        result.Id.Should().BeNull();
        result.Steps.Should().HaveCount(4);
        result.Steps[0].DisplayName.Should().Be("Plan/PO");
    }

    [Fact]
    public async Task Handle_ShouldReturnStepsOrderedBySequenceAscending()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var warehouseId = Guid.NewGuid();
        var customConfig = new InboundWorkflowConfig(TenantA, warehouseId, null, null, allowOverReceive: true, overReceiveTolerancePercentage: null);

        // Add steps out of sequence order
        customConfig.UpdateSteps(new List<InboundStepDefinition>
        {
            new(InboundStepType.Putaway, 2, "Step 3 - Putaway"),
            new(InboundStepType.PO, 0, "Step 1 - PO"),
            new(InboundStepType.Receive, 1, "Step 2 - Receive")
        });

        db.Set<InboundWorkflowConfig>().Add(customConfig);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var uow = CreateUnitOfWork(db);
        var handler = new GetInboundWorkflowConfigQueryHandler(uow);

        // Act
        var result = await handler.Handle(
            new GetInboundWorkflowConfigQuery(TenantA, warehouseId),
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Steps.Should().HaveCount(3);
        result.Steps[0].Sequence.Should().Be(0);
        result.Steps[0].StepType.Should().Be(InboundStepType.PO);
        result.Steps[1].Sequence.Should().Be(1);
        result.Steps[1].StepType.Should().Be(InboundStepType.Receive);
        result.Steps[2].Sequence.Should().Be(2);
        result.Steps[2].StepType.Should().Be(InboundStepType.Putaway);
    }
}
