using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WMS.Application.Common.Models;
using WMS.Application.Inbound.Commands.UpdateInboundWorkflowConfig;
using WMS.Domain.Common;
using WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Inbound;

public sealed class UpdateInboundWorkflowConfigCommandHandlerTests
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        return connection;
    }

    private static WmsDbContext CreateDbContext(SqliteConnection connection, Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(connection)
            .Options;

        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.TenantId).Returns(tenantId);
        currentUserMock.Setup(u => u.Email).Returns("test@example.com");

        return new WmsDbContext(options, currentUserMock.Object, Mock.Of<MediatR.IMediator>());
    }

    private static IUnitOfWork CreateUnitOfWork(WmsDbContext db)
    {
        return new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
    }

    [Fact]
    public async Task Handle_WithValidData_UpdatesInboundWorkflowConfig()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection, TenantA);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Seed existing InboundWorkflowConfig with Putaway step
        var config = new InboundWorkflowConfig(TenantA, Guid.NewGuid(), null, null, allowOverReceive: false, overReceiveTolerancePercentage: null);
        config.UpdateSteps(new List<InboundWorkflowStep>
        {
            new(InboundStepType.Putaway, 0, "Old Putaway")
        });
        db.Set<InboundWorkflowConfig>().Add(config);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var uow = CreateUnitOfWork(db);
        var handler = new UpdateInboundWorkflowConfigCommandHandler(uow);

        var steps = new List<UpdateInboundWorkflowConfigStepDto>
        {
            new(InboundStepType.PO, 0, "New PO"),
            new(InboundStepType.Putaway, 1, "New Putaway")
        };

        var command = new UpdateInboundWorkflowConfigCommand(
            config.Id,
            TenantA,
            true,
            15m,
            steps);

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var updatedConfig = await db.Set<InboundWorkflowConfig>()
            .Include(c => c.Steps)
            .FirstOrDefaultAsync(c => c.Id == config.Id, TestContext.Current.CancellationToken);

        updatedConfig.Should().NotBeNull();
        updatedConfig!.AllowOverReceive.Should().BeTrue();
        updatedConfig.OverReceiveTolerancePercentage.Should().Be(15m);
        updatedConfig.Steps.Should().HaveCount(2);
        updatedConfig.Steps.Any(s => s.StepType == InboundStepType.PO && s.DisplayName == "New PO").Should().BeTrue();
        updatedConfig.Steps.Any(s => s.StepType == InboundStepType.Putaway && s.DisplayName == "New Putaway").Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ThrowsNotFoundException()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection, TenantA);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var uow = CreateUnitOfWork(db);
        var handler = new UpdateInboundWorkflowConfigCommandHandler(uow);

        var steps = new List<UpdateInboundWorkflowConfigStepDto>
        {
            new(InboundStepType.Putaway, 0, "Putaway")
        };

        var command = new UpdateInboundWorkflowConfigCommand(
            Guid.NewGuid(), // Non-existent
            TenantA,
            true,
            null,
            steps);

        // Act
        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithDifferentTenant_ThrowsNotFoundException()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection, TenantA);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Seed existing InboundWorkflowConfig for Tenant B
        var config = new InboundWorkflowConfig(TenantB, Guid.NewGuid(), null, null, allowOverReceive: false, overReceiveTolerancePercentage: null);
        config.UpdateSteps(new List<InboundWorkflowStep> { new(InboundStepType.Putaway, 0, "Putaway") });
        db.Set<InboundWorkflowConfig>().Add(config);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Setup handler with database context configured for Tenant A
        var uow = CreateUnitOfWork(db);
        var handler = new UpdateInboundWorkflowConfigCommandHandler(uow);

        var steps = new List<UpdateInboundWorkflowConfigStepDto>
        {
            new(InboundStepType.Putaway, 0, "Putaway")
        };

        // Attempting to update config of Tenant B using Tenant A
        var command = new UpdateInboundWorkflowConfigCommand(
            config.Id,
            TenantA,
            true,
            null,
            steps);

        // Act
        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
