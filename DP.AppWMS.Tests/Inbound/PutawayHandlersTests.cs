using FluentAssertions;
using Moq;
using System.Linq.Expressions;
using WMS.Application.Common.Service;
using WMS.Application.Inbound.Handlers;
using WMS.Domain.Entities.GoodsReceiptNoteAggregateRoot;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Entities.InboundOrderHistoryAggregateRoot;
using WMS.Domain.Entities.InboundReceiptAggregateRoot;
using WMS.Domain.Entities.InventoryAggregateRoot;
using WMS.Domain.Entities.PutawayTaskAggregateRoot;
using WMS.Domain.Events;
using WMS.Domain.Interfaces;
using WMS.Application.Inbound.Commands.CompletePutaway;
using WMS.Application.Inbound.Commands.CreateDirectPutaway;
using WMS.Application.Inbound.DTOs;
using WMS.Application.Inbound.Validators;
using WMS.Domain.Common;
using WMS.Domain.Entities;
using WMS.Domain.Entities.WarehouseAggregateRoot;
using Microsoft.EntityFrameworkCore;
using WMS.Infrastructure.Persistence;
using WMS.Domain.Entities.PalletAggregateRoot;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Enums;
using MediatR;

namespace DP.AppWMS.Tests.Inbound;

public class PutawayHandlersTests
{
    private readonly Mock<IRepository<InventoryItem>> _inventoryRepoMock;
    private readonly Mock<IRepository<InboundOrder>> _inboundOrderRepoMock;
    private readonly Mock<IRepository<GoodsReceiptNote>> _grnRepoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Guid _tenantId = Guid.NewGuid();

    public PutawayHandlersTests()
    {
        _inventoryRepoMock = new Mock<IRepository<InventoryItem>>();
        _inboundOrderRepoMock = new Mock<IRepository<InboundOrder>>();
        _grnRepoMock = new Mock<IRepository<GoodsReceiptNote>>();
        _uowMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(_tenantId);
        _currentUserMock.Setup(x => x.Email).Returns("test@wms.com");
    }

    [Fact]
    public async Task UpdateInventoryHandler_WhenNoMatchingInventoryExists_ShouldCreateNewInventoryItemAsync()
    {
        // Arrange
        var skuId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var inboundOrderId = Guid.NewGuid();

        var inboundOrder = new InboundOrder
        {
            SupplierId = supplierId
        };
        _inboundOrderRepoMock
            .Setup(x => x.GetByIdAsync(inboundOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inboundOrder);

        var task = PutawayTask.Create(_tenantId, "PT-001", inboundOrderId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        task.AddItem(skuId, 10, locationId);
        task.Items.First().CompletePutaway(locationId);

        // FindAsync returns empty list (meaning no match)
        _inventoryRepoMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<InventoryItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryItem>());

        InventoryItem? createdItem = null;
        _inventoryRepoMock
            .Setup(x => x.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryItem, CancellationToken>((item, ct) => createdItem = item)
            .ReturnsAsync((InventoryItem item, CancellationToken ct) => item);

        var handler = new UpdateInventoryHandler(_inventoryRepoMock.Object, _inboundOrderRepoMock.Object, _currentUserMock.Object);
        var notification = new PutawayTaskCompletedEvent(task);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        createdItem.Should().NotBeNull();
        createdItem!.SkuId.Should().Be(skuId);
        createdItem.LocationId.Should().Be(locationId);
        createdItem.SupplierId.Should().Be(supplierId);
        createdItem.Quantity.Should().Be(10);
    }

    [Fact]
    public async Task UpdateInventoryHandler_WhenMatchingInventoryExists_ShouldAddStockToExistingItemAsync()
    {
        // Arrange
        var skuId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var inboundOrderId = Guid.NewGuid();

        var inboundOrder = new InboundOrder
        {
            SupplierId = supplierId
        };
        _inboundOrderRepoMock
            .Setup(x => x.GetByIdAsync(inboundOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inboundOrder);

        var task = PutawayTask.Create(_tenantId, "PT-001", inboundOrderId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        task.AddItem(skuId, 10, locationId);
        task.Items.First().CompletePutaway(locationId);

        var existingItem = InventoryItem.Create(_tenantId, skuId, locationId, supplierId, null, null, 25, 0m, DateTime.UtcNow, null);

        // FindAsync returns the existing item
        _inventoryRepoMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<InventoryItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryItem> { existingItem });

        var handler = new UpdateInventoryHandler(_inventoryRepoMock.Object, _inboundOrderRepoMock.Object, _currentUserMock.Object);
        var notification = new PutawayTaskCompletedEvent(task);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        existingItem.Quantity.Should().Be(35); // 25 + 10
        _inventoryRepoMock.Verify(x => x.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _inventoryRepoMock.Verify(x => x.UpdateAsync(existingItem), Times.Once);
    }

    [Fact]
    public async Task GenerateGrnHandler_ShouldCreateGoodsReceiptNoteAsync()
    {
        // Arrange
        var skuId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var task = PutawayTask.Create(_tenantId, "PT-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        task.AddItem(skuId, 10, locationId);
        task.Items.First().CompletePutaway(locationId);

        GoodsReceiptNote? generatedGrn = null;
        _grnRepoMock
            .Setup(x => x.AddAsync(It.IsAny<GoodsReceiptNote>(), It.IsAny<CancellationToken>()))
            .Callback<GoodsReceiptNote, CancellationToken>((grn, ct) => generatedGrn = grn)
            .ReturnsAsync((GoodsReceiptNote grn, CancellationToken ct) => grn);

        var sequenceCodeGeneratorMock = new Mock<ISequenceCodeGenerator>();
        sequenceCodeGeneratorMock
            .Setup(x => x.NextAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("GRN-TEST");

        var handler = new GenerateGrnHandler(_grnRepoMock.Object, sequenceCodeGeneratorMock.Object);
        var notification = new PutawayTaskCompletedEvent(task);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        generatedGrn.Should().NotBeNull();
        generatedGrn!.PutawayTaskId.Should().Be(task.Id);
        generatedGrn.Items.Should().HaveCount(1);
        generatedGrn.Items.First().SkuId.Should().Be(skuId);
        generatedGrn.Items.First().Quantity.Should().Be(10);
    }

    [Fact]
    public async Task InboundReceiptCompletedEventHandler_ShouldAddHistoryAsync()
    {
        var historyRepoMock = new Mock<IRepository<InboundOrderHistory>>();
        InboundOrderHistory? loggedHistory = null;
        historyRepoMock
            .Setup(x => x.AddAsync(It.IsAny<InboundOrderHistory>(), It.IsAny<CancellationToken>()))
            .Callback<InboundOrderHistory, CancellationToken>((h, ct) => loggedHistory = h)
            .ReturnsAsync((InboundOrderHistory h, CancellationToken ct) => h);

        var receipt = new InboundReceipt("REC-001", Guid.NewGuid(), Guid.NewGuid());
        var handler = new InboundReceiptCompletedEventHandler(historyRepoMock.Object, _currentUserMock.Object);
        var notification = new InboundReceiptCompletedEvent(receipt);

        await handler.Handle(notification, CancellationToken.None);

        loggedHistory.Should().NotBeNull();
        loggedHistory!.InboundOrderId.Should().Be(receipt.InboundOrderId!.Value);
        loggedHistory.Step.Should().Be("Receive");
    }

    [Fact]
    public async Task CompletePutawayCommandHandler_WhenSerialNumberIsProvidedAndQtyIsNotOne_ShouldThrowAppExceptionAsync()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();
        using var _conn = connection;
        using var _db = db;

        var skuId = Guid.NewGuid();
        var actualLocationId = Guid.NewGuid();
        var putawayTaskId = Guid.NewGuid();

        var task = PutawayTask.Create(_tenantId, "PT-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(task, putawayTaskId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.TenantId))!.SetValue(task, _tenantId);

        task.AddItem(skuId, 10, Guid.NewGuid()); // Qty is 10

        // Create and seed target location
        var targetLocation = LocationEntity.Create(_tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "LOC-TEST");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(targetLocation, actualLocationId);
        db.Locations.Add(targetLocation);

        db.PutawayTasks.Add(task);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new CompletePutawayRequest(new List<CompletePutawayItemRequest>
        {
            new CompletePutawayItemRequest(
                SkuId: skuId,
                ActualLocationId: actualLocationId,
                PalletCode: null,
                SupplierId: null,
                ExpiryDate: null,
                SerialNumber: "SN-99999",
                LotNumber: null)
        });

        var handler = new CompletePutawayCommandHandler(uow);
        var command = new CompletePutawayCommand(_tenantId, putawayTaskId, request);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WMS.Application.Common.Models.AppException>()
            .WithMessage("*Quantity must be 1 when Serial Number is specified*");
    }

    [Fact]
    public async Task CompletePutawayCommandHandler_WhenLocationIsBlocked_ShouldThrowAppExceptionAsync()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();
        using var _conn = connection;
        using var _db = db;

        var skuId = Guid.NewGuid();
        var actualLocationId = Guid.NewGuid();
        var putawayTaskId = Guid.NewGuid();

        // Create and block the location
        var location = LocationEntity.Create(_tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "LOC-BLOCKED");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(location, actualLocationId);
        location.SetBlocked(true);
        db.Locations.Add(location);

        var task = PutawayTask.Create(_tenantId, "PT-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(task, putawayTaskId);
        task.AddItem(skuId, 10, Guid.NewGuid());

        db.PutawayTasks.Add(task);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new CompletePutawayRequest(new List<CompletePutawayItemRequest>
        {
            new CompletePutawayItemRequest(
                SkuId: skuId,
                ActualLocationId: actualLocationId,
                PalletCode: null,
                SupplierId: null,
                ExpiryDate: null,
                SerialNumber: null,
                LotNumber: null)
        });

        var handler = new CompletePutawayCommandHandler(uow);
        var command = new CompletePutawayCommand(_tenantId, putawayTaskId, request);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WMS.Application.Common.Models.AppException>()
            .WithMessage("*đang bị khóa*");
    }

    [Fact]
    public async Task CompletePutawayCommandHandler_WhenTargetLocationInWcsBlockAndPalletCodeIsMissing_ShouldThrowDomainExceptionAsync()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();
        using var _conn = connection;
        using var _db = db;

        var skuId = Guid.NewGuid();
        var actualLocationId = Guid.NewGuid();
        var putawayTaskId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var blockId = Guid.NewGuid();

        // Create Warehouse and Area to satisfy FK constraints
        var warehouse = new Warehouse(_tenantId, "WH-01", "WH-01");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(warehouse, warehouseId);
        db.Warehouses.Add(warehouse);

        var area = warehouse.AddArea("Area-01", "A01", false, false);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(area, areaId);
        db.WarehouseAreas.Add(area);

        // Create Block with WcsBlockId
        var block = warehouse.AddBlock(areaId, "B01", "B01", false);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(block, blockId);
        typeof(Block).GetProperty(nameof(Block.WcsBlockId))!.SetValue(block, "WCS-B01");
        db.Blocks.Add(block);

        // Create Location within WCS block
        var location = LocationEntity.Create(_tenantId, warehouseId, areaId, blockId, null, "LOC-WCS");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(location, actualLocationId);
        db.Locations.Add(location);

        var task = PutawayTask.Create(_tenantId, "PT-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(task, putawayTaskId);
        task.AddItem(skuId, 10, Guid.NewGuid());

        db.PutawayTasks.Add(task);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new CompletePutawayRequest(new List<CompletePutawayItemRequest>
        {
            new CompletePutawayItemRequest(
                SkuId: skuId,
                ActualLocationId: actualLocationId,
                PalletCode: null, // Missing PalletCode
                SupplierId: null,
                ExpiryDate: null,
                SerialNumber: null,
                LotNumber: null)
        });

        var handler = new CompletePutawayCommandHandler(uow);
        var command = new CompletePutawayCommand(_tenantId, putawayTaskId, request);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WMS.Domain.Common.DomainException>()
            .WithMessage("*PalletCode is required for automated WCS zones*");
    }

    [Fact]
    public async Task CompletePutawayCommandHandler_WhenTargetLocationInWcsBlockIsOccupied_ShouldThrowAppExceptionAsync()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();
        using var _conn = connection;
        using var _db = db;

        var skuId = Guid.NewGuid();
        var actualLocationId = Guid.NewGuid();
        var putawayTaskId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var blockId = Guid.NewGuid();

        // Create Warehouse and Area to satisfy FK constraints
        var warehouse = new Warehouse(_tenantId, "WH-01", "WH-01");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(warehouse, warehouseId);
        db.Warehouses.Add(warehouse);

        var area = warehouse.AddArea("Area-01", "A01", false, false);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(area, areaId);
        db.WarehouseAreas.Add(area);

        // Create Block with WcsBlockId
        var block = warehouse.AddBlock(areaId, "B01", "B01", false);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(block, blockId);
        typeof(Block).GetProperty(nameof(Block.WcsBlockId))!.SetValue(block, "WCS-B01");
        db.Blocks.Add(block);

        // Create Location within WCS block
        var location = LocationEntity.Create(_tenantId, warehouseId, areaId, blockId, null, "LOC-WCS");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(location, actualLocationId);
        db.Locations.Add(location);

        // Add active inventory at this location (Quantity > 0)
        var activeInventory = InventoryItem.Create(_tenantId, skuId, actualLocationId, null, null, Guid.NewGuid(), 5, 10.0m, DateTime.UtcNow, null);
        db.InventoryItems.Add(activeInventory);

        var task = PutawayTask.Create(_tenantId, "PT-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(task, putawayTaskId);
        task.AddItem(skuId, 10, Guid.NewGuid());

        db.PutawayTasks.Add(task);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new CompletePutawayRequest(new List<CompletePutawayItemRequest>
        {
            new CompletePutawayItemRequest(
                SkuId: skuId,
                ActualLocationId: actualLocationId,
                PalletCode: "PL-001",
                SupplierId: null,
                ExpiryDate: null,
                SerialNumber: null,
                LotNumber: null)
        });

        var handler = new CompletePutawayCommandHandler(uow);
        var command = new CompletePutawayCommand(_tenantId, putawayTaskId, request);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WMS.Application.Common.Models.AppException>()
            .WithMessage("*đã có hàng tồn kho*");
    }

    [Fact]
    public async Task CompletePutawayCommandHandler_WhenSerialNumberAlreadyExistsInActiveInventory_ShouldThrowAppExceptionAsync()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();
        using var _conn = connection;
        using var _db = db;

        var skuId = Guid.NewGuid();
        var actualLocationId = Guid.NewGuid();
        var putawayTaskId = Guid.NewGuid();

        // 1. Create an existing inventory item with the same serial number in active stock (Quantity = 1)
        var existingInventory = InventoryItem.Create(
            _tenantId,
            skuId,
            actualLocationId,
            null,
            "SN-UNIQUE-123",
            null,
            1, // Quantity = 1
            10.0m,
            DateTime.UtcNow,
            null
        );
        db.InventoryItems.Add(existingInventory);

        // 2. Create the putaway task to be completed with the same serial number
        var task = PutawayTask.Create(_tenantId, "PT-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(task, putawayTaskId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.TenantId))!.SetValue(task, _tenantId);

        task.AddItem(skuId, 1, Guid.NewGuid()); // Quantity must be 1 for serial number

        // Create and seed target location
        var targetLocation = LocationEntity.Create(_tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "LOC-TEST");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(targetLocation, actualLocationId);
        db.Locations.Add(targetLocation);

        db.PutawayTasks.Add(task);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new CompletePutawayRequest(new List<CompletePutawayItemRequest>
        {
            new CompletePutawayItemRequest(
                SkuId: skuId,
                ActualLocationId: actualLocationId,
                PalletCode: null,
                SupplierId: null,
                ExpiryDate: null,
                SerialNumber: "SN-UNIQUE-123", // Duplicate serial number
                LotNumber: null)
        });

        var handler = new CompletePutawayCommandHandler(uow);
        var command = new CompletePutawayCommand(_tenantId, putawayTaskId, request);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WMS.Application.Common.Models.AppException>()
            .WithMessage("*Serial number already exists in active inventory*");
    }

    [Fact]
    public async Task CompletePutawayCommandHandler_WhenPalletDoesNotExist_ShouldThrowAppExceptionAsync()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();
        using var _conn = connection;
        using var _db = db;

        var skuId = Guid.NewGuid();
        var actualLocationId = Guid.NewGuid();
        var putawayTaskId = Guid.NewGuid();

        var task = PutawayTask.Create(_tenantId, "PT-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(task, putawayTaskId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.TenantId))!.SetValue(task, _tenantId);

        task.AddItem(skuId, 5, Guid.NewGuid());

        // Create and seed target location
        var targetLocation = LocationEntity.Create(_tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "LOC-TEST");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(targetLocation, actualLocationId);
        db.Locations.Add(targetLocation);

        db.PutawayTasks.Add(task);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new CompletePutawayRequest(new List<CompletePutawayItemRequest>
        {
            new CompletePutawayItemRequest(
                SkuId: skuId,
                ActualLocationId: actualLocationId,
                PalletCode: "PL-NONEXISTENT", // Pallet does not exist
                SupplierId: null,
                ExpiryDate: null,
                SerialNumber: null,
                LotNumber: null)
        });

        var handler = new CompletePutawayCommandHandler(uow);
        var command = new CompletePutawayCommand(_tenantId, putawayTaskId, request);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WMS.Application.Common.Models.AppException>()
            .WithMessage("*Pallet not found*");
    }

    [Fact]
    public async Task CompletePutawayCommandHandler_WhenPalletExists_ShouldSetPalletIdAsync()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();
        using var _conn = connection;
        using var _db = db;

        var skuId = Guid.NewGuid();
        var actualLocationId = Guid.NewGuid();
        var putawayTaskId = Guid.NewGuid();
        var palletCode = "PL-001";

        // Create Pallet
        var pallet = Pallet.Create(_tenantId, palletCode);
        db.Pallets.Add(pallet);

        var task = PutawayTask.Create(_tenantId, "PT-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(task, putawayTaskId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.TenantId))!.SetValue(task, _tenantId);

        task.AddItem(skuId, 5, Guid.NewGuid());

        // Create and seed target location
        var targetLocation = LocationEntity.Create(_tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "LOC-TEST");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(targetLocation, actualLocationId);
        db.Locations.Add(targetLocation);

        db.PutawayTasks.Add(task);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new CompletePutawayRequest(new List<CompletePutawayItemRequest>
        {
            new CompletePutawayItemRequest(
                SkuId: skuId,
                ActualLocationId: actualLocationId,
                PalletCode: palletCode, // Pallet exists
                SupplierId: null,
                ExpiryDate: null,
                SerialNumber: null,
                LotNumber: null)
        });

        var handler = new CompletePutawayCommandHandler(uow);
        var command = new CompletePutawayCommand(_tenantId, putawayTaskId, request);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedTask = await db.PutawayTasks.Include(p => p.Items).FirstAsync(p => p.Id == putawayTaskId, cancellationToken: TestContext.Current.CancellationToken);
        updatedTask.Items.First().PalletId.Should().Be(pallet.Id);
    }

    [Fact]
    public async Task CompletePutawayCommandHandler_WhenPalletHasDifferentSKUAndIsMixSkuIsFalse_ShouldThrowAppExceptionAsync()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();
        using var _conn = connection;
        using var _db = db;

        var skuId = Guid.NewGuid();
        var otherSkuId = Guid.NewGuid();
        var actualLocationId = Guid.NewGuid();
        var putawayTaskId = Guid.NewGuid();
        var palletCode = "PL-002";

        // Create Pallet with IsMixSku = false
        var pallet = Pallet.Create(_tenantId, palletCode);
        pallet.UpdateMixSkuOption(false);
        db.Pallets.Add(pallet);

        // Put another SKU on the pallet in active inventory (Quantity = 5)
        var existingInventory = InventoryItem.Create(
            _tenantId,
            otherSkuId,
            actualLocationId,
            null,
            null,
            pallet.Id,
            5,
            10.0m,
            DateTime.UtcNow,
            null
        );
        db.InventoryItems.Add(existingInventory);

        var task = PutawayTask.Create(_tenantId, "PT-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(task, putawayTaskId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.TenantId))!.SetValue(task, _tenantId);

        task.AddItem(skuId, 5, Guid.NewGuid());

        // Create and seed target location
        var targetLocation = LocationEntity.Create(_tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "LOC-TEST");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(targetLocation, actualLocationId);
        db.Locations.Add(targetLocation);

        db.PutawayTasks.Add(task);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new CompletePutawayRequest(new List<CompletePutawayItemRequest>
        {
            new CompletePutawayItemRequest(
                SkuId: skuId,
                ActualLocationId: actualLocationId,
                PalletCode: palletCode,
                SupplierId: null,
                ExpiryDate: null,
                SerialNumber: null,
                LotNumber: null)
        });

        var handler = new CompletePutawayCommandHandler(uow);
        var command = new CompletePutawayCommand(_tenantId, putawayTaskId, request);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WMS.Application.Common.Models.AppException>()
            .WithMessage("*Pallet does not allow mixed SKUs*");
    }

    [Fact]
    public async Task CompletePutawayCommandHandler_WhenPalletHasDifferentSKUAndIsMixSkuIsTrue_ShouldSucceedAsync()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();
        using var _conn = connection;
        using var _db = db;

        var skuId = Guid.NewGuid();
        var otherSkuId = Guid.NewGuid();
        var actualLocationId = Guid.NewGuid();
        var putawayTaskId = Guid.NewGuid();
        var palletCode = "PL-003";

        // Create Pallet with IsMixSku = true
        var pallet = Pallet.Create(_tenantId, palletCode);
        pallet.UpdateMixSkuOption(true);
        db.Pallets.Add(pallet);

        // Put another SKU on the pallet in active inventory
        var existingInventory = InventoryItem.Create(
            _tenantId,
            otherSkuId,
            actualLocationId,
            null,
            null,
            pallet.Id,
            5,
            10.0m,
            DateTime.UtcNow,
            null
        );
        db.InventoryItems.Add(existingInventory);

        var task = PutawayTask.Create(_tenantId, "PT-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(task, putawayTaskId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.TenantId))!.SetValue(task, _tenantId);

        task.AddItem(skuId, 5, Guid.NewGuid());

        // Create and seed target location
        var targetLocation = LocationEntity.Create(_tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "LOC-TEST");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(targetLocation, actualLocationId);
        db.Locations.Add(targetLocation);

        db.PutawayTasks.Add(task);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new CompletePutawayRequest(new List<CompletePutawayItemRequest>
        {
            new CompletePutawayItemRequest(
                SkuId: skuId,
                ActualLocationId: actualLocationId,
                PalletCode: palletCode,
                SupplierId: null,
                ExpiryDate: null,
                SerialNumber: null,
                LotNumber: null)
        });

        var handler = new CompletePutawayCommandHandler(uow);
        var command = new CompletePutawayCommand(_tenantId, putawayTaskId, request);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        var updatedTask = await db.PutawayTasks.Include(p => p.Items).FirstAsync(p => p.Id == putawayTaskId, cancellationToken: TestContext.Current.CancellationToken);
        updatedTask.Items.First().PalletId.Should().Be(pallet.Id);
    }

    [Fact]
    public async Task CompletePutawayCommandHandler_WhenPalletHasSameSKUAndIsMixSkuIsFalse_ShouldSucceedAsync()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();
        using var _conn = connection;
        using var _db = db;

        var skuId = Guid.NewGuid();
        var actualLocationId = Guid.NewGuid();
        var putawayTaskId = Guid.NewGuid();
        var palletCode = "PL-004";

        // Create Pallet with IsMixSku = false
        var pallet = Pallet.Create(_tenantId, palletCode);
        pallet.UpdateMixSkuOption(false);
        db.Pallets.Add(pallet);

        // Put the SAME SKU on the pallet in active inventory
        var existingInventory = InventoryItem.Create(
            _tenantId,
            skuId,
            actualLocationId,
            null,
            null,
            pallet.Id,
            5,
            10.0m,
            DateTime.UtcNow,
            null
        );
        db.InventoryItems.Add(existingInventory);

        var task = PutawayTask.Create(_tenantId, "PT-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(task, putawayTaskId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.TenantId))!.SetValue(task, _tenantId);

        task.AddItem(skuId, 5, Guid.NewGuid());

        // Create and seed target location
        var targetLocation = LocationEntity.Create(_tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "LOC-TEST");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(targetLocation, actualLocationId);
        db.Locations.Add(targetLocation);

        db.PutawayTasks.Add(task);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new CompletePutawayRequest(new List<CompletePutawayItemRequest>
        {
            new CompletePutawayItemRequest(
                SkuId: skuId,
                ActualLocationId: actualLocationId,
                PalletCode: palletCode,
                SupplierId: null,
                ExpiryDate: null,
                SerialNumber: null,
                LotNumber: null)
        });

        var handler = new CompletePutawayCommandHandler(uow);
        var command = new CompletePutawayCommand(_tenantId, putawayTaskId, request);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        var updatedTask = await db.PutawayTasks.Include(p => p.Items).FirstAsync(p => p.Id == putawayTaskId, cancellationToken: TestContext.Current.CancellationToken);
        updatedTask.Items.First().PalletId.Should().Be(pallet.Id);
    }

    [Fact]
    public async Task CreateDirectPutawayCommandHandler_WhenValidRequest_ShouldCreateTaskInPendingStatusWithFieldsCorrectly()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();
        using var _conn = connection;
        using var _db = db;

        var skuId = Guid.NewGuid();
        var targetLocationId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var palletCode = "PL-DIR-01";

        // Create SKU so capacity validation succeeds
        var sku = Sku.Create(_tenantId, Guid.NewGuid(), "SKU-DIR-01", "SKU Direct", null, null, 10.0m, null, 0, 100);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(sku, skuId);
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var seqGenMock = new Mock<ISequenceCodeGenerator>();
        seqGenMock.Setup(s => s.NextAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("PT-DIR-001");

        var request = new CreateDirectPutawayRequest(
            _tenantId,
            warehouseId,
            new List<CreateDirectPutawayItemRequest>
            {
                new CreateDirectPutawayItemRequest(
                    SkuId: skuId,
                    Quantity: 10,
                    TargetLocationId: targetLocationId,
                    PalletCode: palletCode,
                    SupplierId: null,
                    ExpiryDate: null,
                    SerialNumber: null,
                    LotNumber: null)
            });

        var handler = new CreateDirectPutawayCommandHandler(uow, seqGenMock.Object, new WMS.Domain.Services.PalletPutawayDomainService());
        var command = new CreateDirectPutawayCommand(_tenantId, request);

        // Act
        var taskId = await handler.Handle(command, CancellationToken.None);

        // Assert
        taskId.Should().NotBeEmpty();
        var createdTask = await db.PutawayTasks.Include(p => p.Items).FirstAsync(p => p.Id == taskId, cancellationToken: TestContext.Current.CancellationToken);
        createdTask.PutawayTaskNumber.Should().Be("PT-DIR-001");
        createdTask.WarehouseId.Should().Be(warehouseId);
        createdTask.Status.Should().Be(WMS.Domain.Enums.PutawayStatus.Pending);
        createdTask.Items.Should().HaveCount(1);

        var taskItem = createdTask.Items.First();
        taskItem.SkuId.Should().Be(skuId);
        taskItem.PutawayQuantity.Should().Be(10);
        taskItem.TargetLocationId.Should().Be(targetLocationId);

        // Pallet should be resolved/created
        var pallet = await db.Pallets.FirstAsync(p => p.PalletCode == palletCode, cancellationToken: TestContext.Current.CancellationToken);
        taskItem.PalletId.Should().Be(pallet.Id);
    }

    [Fact]
    public void CreateDirectPutawayRequestValidator_WhenSerialNumberIsProvidedAndQtyIsNotOne_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new CreateDirectPutawayRequestValidator();
        var request = new CreateDirectPutawayRequest(
            _tenantId,
            Guid.NewGuid(),
            new List<CreateDirectPutawayItemRequest>
            {
                new CreateDirectPutawayItemRequest(
                    SkuId: Guid.NewGuid(),
                    Quantity: 5, // Qty must be 1 when Serial is provided
                    TargetLocationId: Guid.NewGuid(),
                    PalletCode: null,
                    SupplierId: null,
                    ExpiryDate: null,
                    SerialNumber: "SN-DIR-01",
                    LotNumber: null)
            });

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Quantity") && e.ErrorMessage.Contains("Quantity must be 1 when Serial Number is specified"));
    }

    [Fact]
    public async Task CreateDirectPutawayCommandHandler_WhenSerialNumberAlreadyExistsInActiveInventory_ShouldThrowAppExceptionAsync()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();
        using var _conn = connection;
        using var _db = db;

        var serialNumber = "SN-DIR-DUP";

        // Seed duplicate serial in active inventory
        var existingInventory = InventoryItem.Create(
            _tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            serialNumber,
            null,
            1,
            10.0m,
            DateTime.UtcNow,
            null
        );
        db.InventoryItems.Add(existingInventory);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new CreateDirectPutawayRequest(
            _tenantId,
            Guid.NewGuid(),
            new List<CreateDirectPutawayItemRequest>
            {
                new CreateDirectPutawayItemRequest(
                    SkuId: Guid.NewGuid(),
                    Quantity: 1,
                    TargetLocationId: Guid.NewGuid(),
                    PalletCode: null,
                    SupplierId: null,
                    ExpiryDate: null,
                    SerialNumber: serialNumber,
                    LotNumber: null)
            });

        var handler = new CreateDirectPutawayCommandHandler(uow, Mock.Of<ISequenceCodeGenerator>(), new WMS.Domain.Services.PalletPutawayDomainService());
        var command = new CreateDirectPutawayCommand(_tenantId, request);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WMS.Application.Common.Models.AppException>()
            .WithMessage("*Serial number already exists in active inventory*");
    }

    [Fact]
    public async Task CreateDirectPutawayCommandHandler_WhenPalletHasDifferentSKUAndIsMixSkuIsFalse_ShouldThrowAppException()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();
        using var _conn = connection;
        using var _db = db;

        var skuId = Guid.NewGuid();
        var otherSkuId = Guid.NewGuid();
        var palletCode = "PL-DIR-NOMIX";

        // Create Pallet with IsMixSku = false
        var pallet = Pallet.Create(_tenantId, palletCode);
        pallet.UpdateMixSkuOption(false);
        db.Pallets.Add(pallet);

        // Put another SKU on the pallet in active inventory
        var existingInventory = InventoryItem.Create(
            _tenantId,
            otherSkuId,
            Guid.NewGuid(),
            null,
            null,
            pallet.Id,
            5,
            10.0m,
            DateTime.UtcNow,
            null
        );
        db.InventoryItems.Add(existingInventory);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new CreateDirectPutawayRequest(
            _tenantId,
            Guid.NewGuid(),
            new List<CreateDirectPutawayItemRequest>
            {
                new CreateDirectPutawayItemRequest(
                    SkuId: skuId,
                    Quantity: 5,
                    TargetLocationId: Guid.NewGuid(),
                    PalletCode: palletCode,
                    SupplierId: null,
                    ExpiryDate: null,
                    SerialNumber: null,
                    LotNumber: null)
            });

        var handler = new CreateDirectPutawayCommandHandler(uow, Mock.Of<ISequenceCodeGenerator>(), new WMS.Domain.Services.PalletPutawayDomainService());
        var command = new CreateDirectPutawayCommand(_tenantId, request);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WMS.Application.Common.Models.AppException>()
            .WithMessage("*Pallet does not allow mixed SKUs*");
    }

    [Fact]
    public async Task CreateDirectPutawayCommandHandler_WhenAddingExceedsMaxQtyInPallet_ShouldThrowDomainException()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();
        using var _conn = connection;
        using var _db = db;

        var skuId = Guid.NewGuid();
        var palletCode = "PL-DIR-CAP";

        // Create Pallet
        var pallet = Pallet.Create(_tenantId, palletCode);
        db.Pallets.Add(pallet);

        // Create SKU with MaxQtyInPallet = 10
        var sku = Sku.Create(_tenantId, Guid.NewGuid(), "SKU-CAP-01", "SKU Capacity", null, null, 10.0m, null, 0, 10);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(sku, skuId);
        db.Skus.Add(sku);

        // Put 8 items of this SKU on pallet already
        var existingInventory = InventoryItem.Create(
            _tenantId,
            skuId,
            Guid.NewGuid(),
            null,
            null,
            pallet.Id,
            8,
            10.0m,
            DateTime.UtcNow,
            null
        );
        db.InventoryItems.Add(existingInventory);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new CreateDirectPutawayRequest(
            _tenantId,
            Guid.NewGuid(),
            new List<CreateDirectPutawayItemRequest>
            {
                new CreateDirectPutawayItemRequest(
                    SkuId: skuId,
                    Quantity: 5, // 8 + 5 = 13 > 10 capacity!
                    TargetLocationId: Guid.NewGuid(),
                    PalletCode: palletCode,
                    SupplierId: null,
                    ExpiryDate: null,
                    SerialNumber: null,
                    LotNumber: null)
            });

        var handler = new CreateDirectPutawayCommandHandler(uow, Mock.Of<ISequenceCodeGenerator>(), new WMS.Domain.Services.PalletPutawayDomainService());
        var command = new CreateDirectPutawayCommand(_tenantId, request);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WMS.Domain.Common.DomainException>()
            .WithMessage("*Adding quantity exceeds the maximum pallet capacity*");
    }

    [Fact]
    public async Task CreateDirectPutawayCommandHandler_WhenPalletCodeIsEmpty_ShouldGeneratePalletCodeUsingCodeSequence()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();
        using var _conn = connection;
        using var _db = db;

        var skuId = Guid.NewGuid();
        var targetLocationId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        // Create SKU so capacity validation succeeds
        var sku = Sku.Create(_tenantId, Guid.NewGuid(), "SKU-DIR-02", "SKU Direct 2", null, null, 10.0m, null, 0, 100);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(sku, skuId);
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var seqGenMock = new Mock<ISequenceCodeGenerator>();
        seqGenMock.Setup(s => s.NextAsync(_tenantId, CodeSequenceTypes.PutawayTask, It.IsAny<CancellationToken>()))
            .ReturnsAsync("PT-DIR-002");
        seqGenMock.Setup(s => s.NextAsync(_tenantId, CodeSequenceTypes.Pallet, It.IsAny<CancellationToken>()))
            .ReturnsAsync("PLT000001");

        var request = new CreateDirectPutawayRequest(
            _tenantId,
            warehouseId,
            new List<CreateDirectPutawayItemRequest>
            {
                new CreateDirectPutawayItemRequest(
                    SkuId: skuId,
                    Quantity: 10,
                    TargetLocationId: targetLocationId,
                    PalletCode: null, // Empty pallet code triggers CodeSequence generation
                    SupplierId: null,
                    ExpiryDate: null,
                    SerialNumber: null,
                    LotNumber: null)
            });

        var handler = new CreateDirectPutawayCommandHandler(uow, seqGenMock.Object, new WMS.Domain.Services.PalletPutawayDomainService());
        var command = new CreateDirectPutawayCommand(_tenantId, request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        var task = await db.PutawayTasks.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == result, cancellationToken: TestContext.Current.CancellationToken);
        task.Should().NotBeNull();
        task!.Items.Should().HaveCount(1);

        var palletId = task.Items.First().PalletId;
        palletId.Should().NotBeNull();

        var pallet = await db.Pallets.FirstOrDefaultAsync(p => p.Id == palletId, cancellationToken: TestContext.Current.CancellationToken);
        pallet.Should().NotBeNull();
        pallet!.PalletCode.Should().Be("PLT000001");
    }

    [Fact]
    public async Task CompletePutawayCommandHandler_WhenTargetLocationInWcsBlock_ShouldRaiseWcsTaskRequiredEventAsync()
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();
        var (connection, db, uow) = await SetupInMemoryDbAsync(mediatorMock.Object);
        using var _conn = connection;
        using var _db = db;

        var skuId = Guid.NewGuid();
        var actualLocationId = Guid.NewGuid();
        var putawayTaskId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var palletCode = "PL-WCS-001";

        // Create Warehouse and Area to satisfy FK constraints
        var warehouse = new Warehouse(_tenantId, "WH-WCS", "WH-WCS");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(warehouse, warehouseId);
        db.Warehouses.Add(warehouse);

        var area = warehouse.AddArea("Area-WCS", "A-WCS", false, false);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(area, areaId);
        db.WarehouseAreas.Add(area);

        // Create Block with WcsBlockId
        var block = warehouse.AddBlock(areaId, "B-WCS", "B-WCS", false);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(block, blockId);
        typeof(Block).GetProperty(nameof(Block.WcsBlockId))!.SetValue(block, "WCS-B01");
        db.Blocks.Add(block);

        // Create Location within WCS block with coordinates (1, 2, 3) -> "3.1.2"
        var location = LocationEntity.Create(_tenantId, warehouseId, areaId, blockId, null, "LOC-WCS-TEST", 1, 2, 3);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(location, actualLocationId);
        db.Locations.Add(location);

        // Create Pallet
        var pallet = Pallet.Create(_tenantId, palletCode);
        db.Pallets.Add(pallet);

        var task = PutawayTask.Create(_tenantId, "PT-WCS-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), warehouseId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(task, putawayTaskId);
        task.AddItem(skuId, 10, Guid.NewGuid());

        db.PutawayTasks.Add(task);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new CompletePutawayRequest(new List<CompletePutawayItemRequest>
        {
            new CompletePutawayItemRequest(
                SkuId: skuId,
                ActualLocationId: actualLocationId,
                PalletCode: palletCode,
                SupplierId: null,
                ExpiryDate: null,
                SerialNumber: null,
                LotNumber: "LOT-01")
        });

        var handler = new CompletePutawayCommandHandler(uow);
        var command = new CompletePutawayCommand(_tenantId, putawayTaskId, request);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        mediatorMock.Verify(m => m.Publish<DomainEvent>(
            It.Is<DomainEvent>(e => 
                e is WcsTaskRequiredEvent &&
                ((WcsTaskRequiredEvent)e).TenantId == _tenantId &&
                ((WcsTaskRequiredEvent)e).WarehouseId == warehouseId &&
                ((WcsTaskRequiredEvent)e).PutawayTaskId == putawayTaskId &&
                ((WcsTaskRequiredEvent)e).Items.Count == 1 &&
                ((WcsTaskRequiredEvent)e).Items[0].PalletCode == palletCode &&
                ((WcsTaskRequiredEvent)e).Items[0].ToLocationCode == "3.1.2" &&
                ((WcsTaskRequiredEvent)e).Items[0].WcsBlockId == "WCS-B01" &&
                ((WcsTaskRequiredEvent)e).Items[0].LocationId == actualLocationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task<(Microsoft.Data.Sqlite.SqliteConnection Connection, WmsDbContext Db, UnitOfWork Uow)> SetupInMemoryDbAsync(MediatR.IMediator? mediator = null)
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new WmsDbContext(options, Mock.Of<ICurrentUser>(), mediator ?? Mock.Of<MediatR.IMediator>());
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var uow = new UnitOfWork(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<UnitOfWork>.Instance);
        return (connection, db, uow);
    }
}


