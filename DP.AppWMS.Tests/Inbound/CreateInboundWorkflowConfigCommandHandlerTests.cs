using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WMS.Application.Common.Models;
using WMS.Application.Inbound.Commands.CreateInboundWorkflowConfig;
using WMS.Domain.Common;
using WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;
using WMS.Domain.Entities.WarehouseAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Inbound;

public sealed class CreateInboundWorkflowConfigCommandHandlerTests
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

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

        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.TenantId).Returns(TenantA);
        currentUserMock.Setup(u => u.Email).Returns("test@example.com");

        return new WmsDbContext(options, currentUserMock.Object, Mock.Of<MediatR.IMediator>());
    }

    private static IUnitOfWork CreateUnitOfWork(WmsDbContext db)
    {
        return new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
    }

    [Fact]
    public async Task Handle_WithValidData_CreatesInboundWorkflowConfigAsync()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Seed Warehouse
        var warehouse = new Warehouse(TenantA, "WH A", "Code A", "Address A");
        db.Set<Warehouse>().Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var uow = CreateUnitOfWork(db);
        var handler = new CreateInboundWorkflowConfigCommandHandler(uow);

        var steps = new List<CreateInboundWorkflowConfigStepDto>
        {
            new(InboundStepType.PO, 0, "Plan PO"),
            new(InboundStepType.Putaway, 1, "Direct Putaway")
        };

        var command = new CreateInboundWorkflowConfigCommand(
            TenantA,
            warehouse.Id,
            null,
            null,
            true,
            10m,
            steps);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeEmpty();

        var savedConfig = await db.Set<InboundWorkflowConfig>()
            .Include(c => c.Steps)
            .FirstOrDefaultAsync(c => c.Id == result, TestContext.Current.CancellationToken);

        savedConfig.Should().NotBeNull();
        savedConfig!.TenantId.Should().Be(TenantA);
        savedConfig.WarehouseId.Should().Be(warehouse.Id);
        savedConfig.SupplierId.Should().BeNull();
        savedConfig.CategoryId.Should().BeNull();
        savedConfig.AllowOverReceive.Should().BeTrue();
        savedConfig.OverReceiveTolerancePercentage.Should().Be(10m);
        savedConfig.Steps.Should().HaveCount(2);
        savedConfig.Steps.Any(s => s.StepType == InboundStepType.PO && s.DisplayName == "Plan PO").Should().BeTrue();
        savedConfig.Steps.Any(s => s.StepType == InboundStepType.Putaway && s.DisplayName == "Direct Putaway").Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonExistentWarehouse_ThrowsNotFoundExceptionAsync()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var uow = CreateUnitOfWork(db);
        var handler = new CreateInboundWorkflowConfigCommandHandler(uow);

        var steps = new List<CreateInboundWorkflowConfigStepDto>
        {
            new(InboundStepType.PO, 0, "Plan PO"),
            new(InboundStepType.Putaway, 1, "Putaway")
        };

        var command = new CreateInboundWorkflowConfigCommand(
            TenantA,
            Guid.NewGuid(), // Non-existent
            null,
            null,
            true,
            null,
            steps);

        // Act
        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithNonExistentSupplier_ThrowsNotFoundExceptionAsync()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Seed Warehouse
        var warehouse = new Warehouse(TenantA, "WH A", "Code A", "Address A");
        db.Set<Warehouse>().Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var uow = CreateUnitOfWork(db);
        var handler = new CreateInboundWorkflowConfigCommandHandler(uow);

        var steps = new List<CreateInboundWorkflowConfigStepDto>
        {
            new(InboundStepType.PO, 0, "Plan PO"),
            new(InboundStepType.Putaway, 1, "Putaway")
        };

        var command = new CreateInboundWorkflowConfigCommand(
            TenantA,
            warehouse.Id,
            Guid.NewGuid(), // Non-existent Supplier
            null,
            true,
            null,
            steps);

        // Act
        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithNonExistentCategory_ThrowsNotFoundExceptionAsync()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Seed Warehouse
        var warehouse = new Warehouse(TenantA, "WH A", "Code A", "Address A");
        db.Set<Warehouse>().Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var uow = CreateUnitOfWork(db);
        var handler = new CreateInboundWorkflowConfigCommandHandler(uow);

        var steps = new List<CreateInboundWorkflowConfigStepDto>
        {
            new(InboundStepType.PO, 0, "Plan PO"),
            new(InboundStepType.Putaway, 1, "Putaway")
        };

        var command = new CreateInboundWorkflowConfigCommand(
            TenantA,
            warehouse.Id,
            null,
            Guid.NewGuid(), // Non-existent Category
            true,
            null,
            steps);

        // Act
        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithDuplicateConfiguration_ThrowsAppExceptionAsync()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Seed Warehouse
        var warehouse = new Warehouse(TenantA, "WH A", "Code A", "Address A");
        db.Set<Warehouse>().Add(warehouse);

        // Seed existing InboundWorkflowConfig
        var existingConfig = new InboundWorkflowConfig(TenantA, warehouse.Id, null, null, allowOverReceive: true, overReceiveTolerancePercentage: null);
        existingConfig.UpdateSteps(new List<InboundStepDefinition> { new(InboundStepType.Putaway, 0, "Putaway") });
        db.Set<InboundWorkflowConfig>().Add(existingConfig);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var uow = CreateUnitOfWork(db);
        var handler = new CreateInboundWorkflowConfigCommandHandler(uow);

        var steps = new List<CreateInboundWorkflowConfigStepDto>
        {
            new(InboundStepType.PO, 0, "Plan PO"),
            new(InboundStepType.Putaway, 1, "Putaway")
        };

        var command = new CreateInboundWorkflowConfigCommand(
            TenantA,
            warehouse.Id,
            null,
            null,
            true,
            null,
            steps);

        // Act
        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(400);
        exception.Which.Code.Should().Be("DUPLICATE_CONFIG");
    }
}
