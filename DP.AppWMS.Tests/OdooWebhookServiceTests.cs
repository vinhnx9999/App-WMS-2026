using FluentAssertions;
using Moq;
using WMS.Application.Common.Models;
using WMS.Application.OdooIntegration.OdooWebhook;
using WMS.Domain.Entities.ErpSync;
using WMS.Domain.Entities.Inbound;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.ERPs.Odoo.OdooWebhook;

namespace DP.AppWMS.Tests;

public class OdooWebhookServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IRepository<WebhookEvent>> _webhookRepo;
    private readonly Mock<IRepository<InboundOrder>> _inboundRepo;
    private readonly string _ipAddress = "10.0.0.1";

    public OdooWebhookServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _webhookRepo = new Mock<IRepository<WebhookEvent>>();
        _inboundRepo = new Mock<IRepository<InboundOrder>>();

        _uowMock.Setup(x => x.Repository<WebhookEvent>())
            .Returns(_webhookRepo.Object);
        _uowMock.Setup(x => x.Repository<InboundOrder>())
            .Returns(_inboundRepo.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidPayload_ShouldReturnSuccess()
    {
        // Arrange
        var payload = new OdooWebhookPayload
        {
            Event = "picking.validated",
            PickingId = 42,
            PickingName = "WH/IN/00042",
            PickingType = "incoming",
            State = "done",
            Partner = "Samsung Vina",
            Origin = "PO-2025-0001",
        };

        _webhookRepo
            .Setup(x => x.ExistsAsync(It.IsAny<
                System.Linq.Expressions.Expression<Func<WebhookEvent, bool>>>()))
            .ReturnsAsync(false);

        _inboundRepo
            .Setup(x => x.FindAsync(It.IsAny<
                System.Linq.Expressions.Expression<Func<InboundOrder, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new()
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = "WH/IN/00042",
                    Status = InboundStatus.Pending,
                }
            ]);

        // Act
        var svc = new OdooWebhookService(
            _uowMock.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<OdooWebhookService>>());

        CancellationToken ct = CancellationToken.None;
        var result = await svc.HandleAsync(payload, _ipAddress, ct);

        // Assert
        result.Received.Should().BeTrue();
        result.Message.Should().Contain("Processed");

        _webhookRepo.Verify(x => x.AddAsync(
            It.IsAny<WebhookEvent>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _uowMock.Verify(x => x.SaveChangesAsync(
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DuplicatePayload_ShouldSkip()
    {
        // Arrange
        var payload = new OdooWebhookPayload
        {
            Event = "picking.validated",
            PickingId = 42,
            PickingName = "WH/IN/00042",
            PickingType = "incoming",
        };

        _webhookRepo
            .Setup(x => x.ExistsAsync(It.IsAny<
                System.Linq.Expressions.Expression<Func<WebhookEvent, bool>>>()))
            .ReturnsAsync(true);  // Already exists

        // Act
        var svc = new OdooWebhookService(
            _uowMock.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<OdooWebhookService>>());
                
        CancellationToken ct = CancellationToken.None;
        var result = await svc.HandleAsync(payload, _ipAddress, ct);

        // Assert
        result.Received.Should().BeTrue();
        result.Message.Should().Contain("duplicate");

        // Không lưu webhook mới
        _webhookRepo.Verify(x => x.AddAsync(
            It.IsAny<WebhookEvent>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_MissingEvent_ShouldThrow()
    {
        // Arrange
        var payload = new OdooWebhookPayload
        {
            Event = "",  // Missing
            PickingName = "WH/IN/00042",
        };

        // Act & Assert
        var svc = new OdooWebhookService(
            _uowMock.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<OdooWebhookService>>());

        CancellationToken ct = CancellationToken.None;
        var act = () => svc.HandleAsync(payload, _ipAddress, ct);
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.Code == "INVALID_PAYLOAD");
    }
}