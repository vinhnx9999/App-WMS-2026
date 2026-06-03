using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Product.Skus.Commands.DeleteSku;
using WMS.Domain.Entities.Product;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Skus;

public sealed class DeleteSkuCommandHandlerTests : BaseSkuHandlerTest
{
    #region DeleteSkuCommandHandler

    [Fact]
    public async Task Delete_WhenSkuExists_MarksDeletedAndSaves()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sku = product.AddSku(TenantA, "SKU-001", "Phone", null, null, 0m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateDeleteHandler(db).Handle(new DeleteSkuCommand(TenantA, sku.Id), TestContext.Current.CancellationToken);

        var deleted = await db.Skus.SingleAsync(x => x.Id == sku.Id, TestContext.Current.CancellationToken);
        deleted.IsDeleted.Should().BeTrue();
        deleted.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_WhenSkuDoesNotExist_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var act = () => CreateDeleteHandler(db).Handle(new DeleteSkuCommand(TenantA, Guid.NewGuid()), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found");
    }

    [Fact]
    public async Task Delete_WhenSkuBelongsToOtherTenant_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantB, "PROD-001", "Other Tenant Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sku = product.AddSku(TenantB, "SKU-001", "Phone", null, null, 0m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateDeleteHandler(db).Handle(new DeleteSkuCommand(TenantA, sku.Id), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found");
    }

    [Fact]
    public async Task Delete_WhenSkuIsAlreadyDeleted_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sku = product.AddSku(TenantA, "SKU-001", "Phone", null, null, 0m);
        sku.MarkDeleted();
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateDeleteHandler(db).Handle(new DeleteSkuCommand(TenantA, sku.Id), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found");
    }

    [Fact]
    public async Task Delete_WhenRequestedTenantSkuIsDeletedAndOtherTenantSkuActive_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var productA = Product.Create(TenantA, "PROD-001", "Product A");
        var productB = Product.Create(TenantB, "PROD-002", "Product B");
        db.Set<Product>().AddRange(productA, productB);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var requested = productA.AddSku(TenantA, "SKU-001", "Phone", null, null, 0m);
        requested.MarkDeleted();
        var other = productB.AddSku(TenantB, "SKU-001", "Other tenant", null, null, 0m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateDeleteHandler(db).Handle(new DeleteSkuCommand(TenantA, requested.Id), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found");
    }

    [Fact]
    public async Task Delete_WhenUnrelatedSkusExist_DeletesOnlyMatchingSku()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var target = product.AddSku(TenantA, "SKU-001", "Target", null, null, 0m);
        var other = product.AddSku(TenantA, "SKU-002", "Other", null, null, 0m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateDeleteHandler(db).Handle(new DeleteSkuCommand(TenantA, target.Id), TestContext.Current.CancellationToken);

        var rows = await db.Skus.ToListAsync(TestContext.Current.CancellationToken);
        rows.Single(x => x.Id == target.Id).IsDeleted.Should().BeTrue();
        rows.Single(x => x.Id == other.Id).IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_WhenSkuExists_SetsDeletedAt()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sku = product.AddSku(TenantA, "SKU-001", "Phone", null, null, 0m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateDeleteHandler(db).Handle(new DeleteSkuCommand(TenantA, sku.Id), TestContext.Current.CancellationToken);

        var deleted = await db.Skus.SingleAsync(x => x.Id == sku.Id, TestContext.Current.CancellationToken);
        deleted.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Delete_WhenSkuExists_UpdatesUpdatedAt()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sku = product.AddSku(TenantA, "SKU-001", "Phone", null, null, 0m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var beforeDelete = DateTime.UtcNow;

        await CreateDeleteHandler(db).Handle(new DeleteSkuCommand(TenantA, sku.Id), TestContext.Current.CancellationToken);

        var deleted = await db.Skus.SingleAsync(x => x.Id == sku.Id, TestContext.Current.CancellationToken);
        deleted.UpdatedAt.Should().BeAfter(beforeDelete.AddSeconds(-2));
    }

    [Fact]
    public async Task Delete_WhenOtherTenantHasSimilarSku_KeepsOtherTenantSkuActive()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var productA = Product.Create(TenantA, "PROD-001", "Product A");
        var productB = Product.Create(TenantB, "PROD-002", "Product B");
        db.Set<Product>().AddRange(productA, productB);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var target = productA.AddSku(TenantA, "SKU-001", "Target", null, null, 0m);
        var otherTenant = productB.AddSku(TenantB, "SKU-001", "Other tenant", null, null, 0m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateDeleteHandler(db).Handle(new DeleteSkuCommand(TenantA, target.Id), TestContext.Current.CancellationToken);

        var other = await db.Skus.SingleAsync(x => x.Id == otherTenant.Id, TestContext.Current.CancellationToken);
        other.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_WhenCancellationTokenCanceled_ThrowsCancellationException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => CreateDeleteHandler(db).Handle(new DeleteSkuCommand(TenantA, Guid.NewGuid()), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Helper Methods

    private static DeleteSkuCommandHandler CreateDeleteHandler(WmsDbContext db)
    {
        return new DeleteSkuCommandHandler(CreateUnitOfWork(db));
    }

    #endregion
}
