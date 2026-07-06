using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using WMS.Application.Common.Service;
using WMS.Application.Inbound.Handlers;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Entities.InboundReceiptAggregateRoot;
using WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;
using WMS.Domain.Entities.ProductAggregateRoot;
using WMS.Domain.Entities.PutawayTaskAggregateRoot;
using WMS.Domain.Entities.QcInspectionAggregateRoot;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Events;
using WMS.Domain.Interfaces;
using WMS.Domain.Orchestrator;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Inbound;

public class InboundWorkflowHandlersTests
{
    private readonly InboundWorkflowOrchestrator _orchestrator;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ISequenceCodeGenerator> _sequenceCodeGeneratorMock;

    public InboundWorkflowHandlersTests()
    {
        _orchestrator = new InboundWorkflowOrchestrator();
        _currentUserMock = new Mock<ICurrentUser>();
        _sequenceCodeGeneratorMock = new Mock<ISequenceCodeGenerator>();

        _sequenceCodeGeneratorMock
            .Setup(x => x.NextAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid tenantId, string codeType, CancellationToken ct) =>
                codeType == "QcInspection" ? "QC-TEST" : "PT-TEST");
    }

    private async Task<(SqliteConnection Connection, WmsDbContext Db, UnitOfWork Uow)> SetupInMemoryDbAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new WmsDbContext(options, _currentUserMock.Object, Mock.Of<MediatR.IMediator>());
        await db.Database.EnsureCreatedAsync();
        var uow = new UnitOfWork(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<UnitOfWork>.Instance);
        return (connection, db, uow);
    }

    [Fact]
    public async Task HandleInboundReceiptCompletedEvent_ShouldCreateQcAndPutawayDocumentsBasedOnConfigAsync()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();

        var warehouseId = Guid.NewGuid();
        var inboundOrderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Product categories
        var cat1 = Guid.NewGuid();
        var cat2 = Guid.NewGuid();

        var prod1Id = Guid.NewGuid();
        var prod2Id = Guid.NewGuid();

        var prod1 = Product.Create(tenantId, "PROD1", "Product 1", null, cat1);
        typeof(WMS.Domain.Common.BaseEntity).GetProperty(nameof(WMS.Domain.Common.BaseEntity.Id))!.SetValue(prod1, prod1Id);

        var prod2 = Product.Create(tenantId, "PROD2", "Product 2", null, cat2);
        typeof(WMS.Domain.Common.BaseEntity).GetProperty(nameof(WMS.Domain.Common.BaseEntity.Id))!.SetValue(prod2, prod2Id);

        db.Products.Add(prod1);
        db.Products.Add(prod2);

        var sku1Id = Guid.NewGuid();
        var sku2Id = Guid.NewGuid();

        var sku1 = Sku.Create(tenantId, prod1Id, "SKU1", "Sku 1", null, null, 10m);
        typeof(WMS.Domain.Common.BaseEntity).GetProperty(nameof(WMS.Domain.Common.BaseEntity.Id))!.SetValue(sku1, sku1Id);

        var sku2 = Sku.Create(tenantId, prod2Id, "SKU2", "Sku 2", null, null, 10m);
        typeof(WMS.Domain.Common.BaseEntity).GetProperty(nameof(WMS.Domain.Common.BaseEntity.Id))!.SetValue(sku2, sku2Id);

        db.Skus.Add(sku1);
        db.Skus.Add(sku2);

        var supplierId = Guid.NewGuid();
        var inboundOrder = InboundOrder.Create(tenantId, "PO-001", null, null);
        typeof(WMS.Domain.Common.BaseEntity).GetProperty(nameof(WMS.Domain.Common.BaseEntity.Id))!.SetValue(inboundOrder, inboundOrderId);
        inboundOrder.AddItem(sku1Id, 10, supplierId);
        inboundOrder.AddItem(sku2Id, 20, supplierId);
        db.InboundOrders.Add(inboundOrder);

        // Config 1: PO -> Receive -> QC -> Putaway (For Sku1 category)
        var configQC = new InboundWorkflowConfig(tenantId, warehouseId, supplierId, cat1);
        configQC.UpdateSteps(new List<InboundStepDefinition>
        {
            new(InboundStepType.PO, 0, "PO"),
            new(InboundStepType.Receive, 1, "Receive"),
            new(InboundStepType.QC, 2, "QC"),
            new(InboundStepType.Putaway, 3, "Putaway")
        });

        // Config 2: PO -> Receive -> Putaway (Bypasses QC for Sku2 category)
        var configNoQC = new InboundWorkflowConfig(tenantId, warehouseId, supplierId, cat2);
        configNoQC.UpdateSteps(new List<InboundStepDefinition>
        {
            new(InboundStepType.PO, 0, "PO"),
            new(InboundStepType.Receive, 1, "Receive"),
            new(InboundStepType.Putaway, 2, "Putaway")
        });

        db.InboundWorkflowConfigs.Add(configQC);
        db.InboundWorkflowConfigs.Add(configNoQC);

        await db.SaveChangesAsync();

        var receipt = new InboundReceipt(tenantId, "REC-001", inboundOrderId, warehouseId);
        receipt.AddItem(sku1Id, 10, 10, "QC path");
        receipt.AddItem(sku2Id, 20, 20, "Putaway path");

        var handlers = new InboundWorkflowHandlers(
            uow.Repository<InboundWorkflowConfig>(),
            uow.Repository<InboundOrder>(),
            uow.Repository<Sku>(),
            uow.Repository<Product>(),
            uow.Repository<QcInspection>(),
            uow.Repository<PutawayTask>(),
            _orchestrator,
            _currentUserMock.Object,
            _sequenceCodeGeneratorMock.Object);

        var notification = new CreateInboundReceiptEvent(receipt);

        // Act
        await handlers.Handle(notification, CancellationToken.None);
        await db.SaveChangesAsync();

        // Assert
        var createdQc = await db.QcInspections.Include(q => q.Items).FirstOrDefaultAsync();
        createdQc.Should().NotBeNull();
        createdQc!.Items.Should().HaveCount(1);
        createdQc.Items.First().SkuId.Should().Be(sku1Id);

        var createdPutaway = await db.PutawayTasks.Include(p => p.Items).FirstOrDefaultAsync();
        createdPutaway.Should().NotBeNull();
        createdPutaway!.Items.Should().HaveCount(1);
        createdPutaway.Items.First().SkuId.Should().Be(sku2Id);
    }
}
