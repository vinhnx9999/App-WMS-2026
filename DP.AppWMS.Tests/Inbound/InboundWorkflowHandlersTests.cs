using FluentAssertions;
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


namespace DP.AppWMS.Tests.Inbound;

public class InboundWorkflowHandlersTests
{
    private readonly Mock<IRepository<InboundWorkflowConfig>> _configRepoMock;
    private readonly Mock<IRepository<InboundOrder>> _inboundOrderRepoMock;
    private readonly Mock<IRepository<Sku>> _skuRepoMock;
    private readonly Mock<IRepository<Product>> _productRepoMock;
    private readonly Mock<IRepository<QcInspection>> _qcRepoMock;
    private readonly Mock<IRepository<PutawayTask>> _putawayRepoMock;
    private readonly InboundWorkflowOrchestrator _orchestrator;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ISequenceCodeGenerator> _sequenceCodeGeneratorMock;
    private readonly InboundWorkflowHandlers _handlers;

    public InboundWorkflowHandlersTests()
    {
        _configRepoMock = new Mock<IRepository<InboundWorkflowConfig>>();
        _inboundOrderRepoMock = new Mock<IRepository<InboundOrder>>();
        _skuRepoMock = new Mock<IRepository<Sku>>();
        _productRepoMock = new Mock<IRepository<Product>>();
        _qcRepoMock = new Mock<IRepository<QcInspection>>();
        _putawayRepoMock = new Mock<IRepository<PutawayTask>>();
        _orchestrator = new InboundWorkflowOrchestrator();
        _currentUserMock = new Mock<ICurrentUser>();
        _sequenceCodeGeneratorMock = new Mock<ISequenceCodeGenerator>();

        _sequenceCodeGeneratorMock
            .Setup(x => x.NextAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid tenantId, string codeType, CancellationToken ct) =>
                codeType == "QcInspection" ? "QC-TEST" : "PT-TEST");

        _handlers = new InboundWorkflowHandlers(
            _configRepoMock.Object,
            _inboundOrderRepoMock.Object,
            _skuRepoMock.Object,
            _productRepoMock.Object,
            _qcRepoMock.Object,
            _putawayRepoMock.Object,
            _orchestrator,
            _currentUserMock.Object,
            _sequenceCodeGeneratorMock.Object);
    }

    [Fact]
    public async Task HandleInboundReceiptCompletedEvent_ShouldCreateQcAndPutawayDocumentsBasedOnConfigAsync()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var inboundOrderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var supplierId = Guid.NewGuid();
        var inboundOrder = new InboundOrder { SupplierId = supplierId };
        _inboundOrderRepoMock
            .Setup(x => x.GetByIdAsync(inboundOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inboundOrder);

        var sku1 = Sku.Create(tenantId, Guid.NewGuid(), "SKU1", "Sku 1", null, null, 10m);
        var sku2 = Sku.Create(tenantId, Guid.NewGuid(), "SKU2", "Sku 2", null, null, 10m);

        _skuRepoMock.Setup(x => x.GetByIdAsync(sku1.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sku1);
        _skuRepoMock.Setup(x => x.GetByIdAsync(sku2.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sku2);

        // Product categories
        var cat1 = Guid.NewGuid();
        var cat2 = Guid.NewGuid();

        var prod1 = Product.Create(tenantId, "PROD1", "Product 1", null, cat1);
        var prod2 = Product.Create(tenantId, "PROD2", "Product 2", null, cat2);

        _productRepoMock.Setup(x => x.GetByIdAsync(sku1.ProductId!.Value, It.IsAny<CancellationToken>())).ReturnsAsync(prod1);
        _productRepoMock.Setup(x => x.GetByIdAsync(sku2.ProductId!.Value, It.IsAny<CancellationToken>())).ReturnsAsync(prod2);

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

        _configRepoMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InboundWorkflowConfig> { configQC, configNoQC });

        var receipt = new InboundReceipt("REC-001", inboundOrderId, warehouseId);
        var item1 = new InboundReceiptItem(sku1.Id, 10, 10, "QC path");
        var item2 = new InboundReceiptItem(sku2.Id, 20, 20, "Putaway path");
        receipt.AddItem(item1);
        receipt.AddItem(item2);

        QcInspection? createdQc = null;
        _qcRepoMock
            .Setup(x => x.AddAsync(It.IsAny<QcInspection>(), It.IsAny<CancellationToken>()))
            .Callback<QcInspection, CancellationToken>((qc, ct) => createdQc = qc)
            .ReturnsAsync((QcInspection qc, CancellationToken ct) => qc);

        PutawayTask? createdPutaway = null;
        _putawayRepoMock
            .Setup(x => x.AddAsync(It.IsAny<PutawayTask>(), It.IsAny<CancellationToken>()))
            .Callback<PutawayTask, CancellationToken>((pt, ct) => createdPutaway = pt)
            .ReturnsAsync((PutawayTask pt, CancellationToken ct) => pt);

        var notification = new InboundReceiptCompletedEvent(receipt);

        // Act
        await _handlers.Handle(notification, CancellationToken.None);

        // Assert
        createdQc.Should().NotBeNull();
        createdQc!.Items.Should().HaveCount(1);
        createdQc.Items.First().SkuId.Should().Be(sku1.Id);

        createdPutaway.Should().NotBeNull();
        createdPutaway!.Items.Should().HaveCount(1);
        createdPutaway.Items.First().SkuId.Should().Be(sku2.Id);
    }
}
