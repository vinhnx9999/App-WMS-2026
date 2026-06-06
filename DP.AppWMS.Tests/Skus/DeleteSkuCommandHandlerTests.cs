using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Product.Skus.Commands.DeleteSku;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Skus;

public sealed class DeleteSkuCommandHandlerTests : BaseSkuHandlerTest
{
    [Fact]
    public async Task Delete_WhenSkuExists_MarksDeletedAndSaves()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var sku = await AddTestSku(db, TenantA, "SKU-001", "Phone", ct: TestContext.Current.CancellationToken);

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
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found.");
    }

    [Fact]
    public async Task Delete_WhenSkuBelongsToOtherTenant_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var sku = await AddTestSku(db, TenantB, "SKU-001", "Phone", ct: TestContext.Current.CancellationToken);

        var act = () => CreateDeleteHandler(db).Handle(new DeleteSkuCommand(TenantA, sku.Id), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found.");
    }

    [Fact]
    public async Task Delete_WhenSkuIsAlreadyDeleted_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var sku = await AddTestSku(db, TenantA, "SKU-001", "Phone", ct: TestContext.Current.CancellationToken);
        sku.MarkDeleted();
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateDeleteHandler(db).Handle(new DeleteSkuCommand(TenantA, sku.Id), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found.");
    }

    [Fact]
    public async Task Delete_WhenRequestedTenantSkuIsDeletedAndOtherTenantSkuActive_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var requested = await AddTestSku(db, TenantA, "SKU-001", "Phone", ct: TestContext.Current.CancellationToken);
        requested.MarkDeleted();
        var other = await AddTestSku(db, TenantB, "SKU-001", "Other tenant", ct: TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateDeleteHandler(db).Handle(new DeleteSkuCommand(TenantA, requested.Id), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found.");
        other.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_WhenUnrelatedSkusExist_DeletesOnlyMatchingSku()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var target = await AddTestSku(db, TenantA, "SKU-001", "Target", ct: TestContext.Current.CancellationToken);
        var other = await AddTestSku(db, TenantA, "SKU-002", "Other", ct: TestContext.Current.CancellationToken);

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
        var sku = await AddTestSku(db, TenantA, "SKU-001", "Phone", ct: TestContext.Current.CancellationToken);

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
        var sku = await AddTestSku(db, TenantA, "SKU-001", "Phone", ct: TestContext.Current.CancellationToken);
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
        var target = await AddTestSku(db, TenantA, "SKU-001", "Target", ct: TestContext.Current.CancellationToken);
        var otherTenant = await AddTestSku(db, TenantB, "SKU-001", "Other tenant", ct: TestContext.Current.CancellationToken);

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

    private static DeleteSkuCommandHandler CreateDeleteHandler(WmsDbContext db)
    {
        return new DeleteSkuCommandHandler(CreateUnitOfWork(db));
    }
}
