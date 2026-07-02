using FluentAssertions;
using WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Orchestrator;

namespace DP.AppWMS.Tests.Inbound;

public class InboundWorkflowOrchestratorTests
{
    private readonly InboundWorkflowOrchestrator _orchestrator = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _supplierId = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();

    private InboundWorkflowConfig CreateConfig(Guid? warehouseId, Guid? supplierId, Guid? categoryId)
    {
        var config = new InboundWorkflowConfig(_tenantId, warehouseId, supplierId, categoryId);
        config.UpdateSteps(new List<InboundStepDefinition>
        {
            new(InboundStepType.PO, 0, "PO"),
            new(InboundStepType.Putaway, 1, "Putaway")
        });
        return config;
    }

    [Fact]
    public void ResolveConfig_ShouldMatchPriority1_ExactMatch()
    {
        // Arrange
        var p1 = CreateConfig(_warehouseId, _supplierId, _categoryId);
        var p2 = CreateConfig(_warehouseId, _supplierId, null);
        var p3 = CreateConfig(_warehouseId, null, _categoryId);
        var p4 = CreateConfig(_warehouseId, null, null);
        var p5 = CreateConfig(null, null, null);

        var configs = new List<InboundWorkflowConfig> { p5, p4, p3, p2, p1 };

        // Act
        var result = _orchestrator.ResolveConfig(_warehouseId, _supplierId, _categoryId, configs);

        // Assert
        result.Should().Be(p1);
    }

    [Fact]
    public void ResolveConfig_ShouldMatchPriority2_WarehouseAndSupplier()
    {
        // Arrange
        var p2 = CreateConfig(_warehouseId, _supplierId, null);
        var p3 = CreateConfig(_warehouseId, null, _categoryId);
        var p4 = CreateConfig(_warehouseId, null, null);
        var p5 = CreateConfig(null, null, null);

        var configs = new List<InboundWorkflowConfig> { p5, p4, p3, p2 };

        // Act
        var result = _orchestrator.ResolveConfig(_warehouseId, _supplierId, _categoryId, configs);

        // Assert
        result.Should().Be(p2);
    }

    [Fact]
    public void ResolveConfig_ShouldMatchPriority3_WarehouseAndCategory()
    {
        // Arrange
        var p3 = CreateConfig(_warehouseId, null, _categoryId);
        var p4 = CreateConfig(_warehouseId, null, null);
        var p5 = CreateConfig(null, null, null);

        var configs = new List<InboundWorkflowConfig> { p5, p4, p3 };

        // Act
        var result = _orchestrator.ResolveConfig(_warehouseId, _supplierId, _categoryId, configs);

        // Assert
        result.Should().Be(p3);
    }

    [Fact]
    public void ResolveConfig_ShouldMatchPriority4_WarehouseOnly()
    {
        // Arrange
        var p4 = CreateConfig(_warehouseId, null, null);
        var p5 = CreateConfig(null, null, null);

        var configs = new List<InboundWorkflowConfig> { p5, p4 };

        // Act
        var result = _orchestrator.ResolveConfig(_warehouseId, _supplierId, _categoryId, configs);

        // Assert
        result.Should().Be(p4);
    }

    [Fact]
    public void ResolveConfig_ShouldMatchPriority5_GlobalDefault()
    {
        // Arrange
        var p5 = CreateConfig(null, null, null);

        var configs = new List<InboundWorkflowConfig> { p5 };

        // Act
        var result = _orchestrator.ResolveConfig(_warehouseId, _supplierId, _categoryId, configs);

        // Assert
        result.Should().Be(p5);
    }

    [Fact]
    public void ResolveConfig_ShouldFallbackToHardcodedDefault_WhenNoConfigsRegistered()
    {
        // Arrange
        var configs = new List<InboundWorkflowConfig>();

        // Act
        var result = _orchestrator.ResolveConfig(_warehouseId, _supplierId, _categoryId, configs);

        // Assert
        result.Should().NotBeNull();
        result.WarehouseId.Should().BeNull();
        result.Steps.Should().HaveCount(4);
    }
}
