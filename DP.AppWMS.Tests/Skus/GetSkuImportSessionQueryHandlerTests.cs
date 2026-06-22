using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Skus.Queries.GetSkuImportSession;
using WMS.Domain.Entities.Product;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Skus;

public sealed class GetSkuImportSessionQueryHandlerTests : BaseSkuHandlerTest
{
    [Fact]
    public async Task Handle_WhenSessionExists_ReturnsSessionWithPagedRows()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // 1. Create a session with multiple rows
        var session = SkuImportSession.Create(TenantA, "test.xlsx");
        session.AddRow(1, "PROD-001", Guid.NewGuid(), "SKU-001", "Name 1", "Nature 1", "Desc 1", 100m, true, null, null);
        session.AddRow(2, "PROD-002", Guid.NewGuid(), "SKU-002", "Name 2", "Nature 2", "Desc 2", 200m, true, null, null);
        session.AddRow(3, "PROD-003", Guid.NewGuid(), "SKU-003", "Name 3", "Nature 3", "Desc 3", 300m, false, "ERR", "Error message");
        db.Set<SkuImportSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetSkuImportSessionQueryHandler(CreateUnitOfWork(db));

        // 2. Query page 1, limit 2
        var query = new GetSkuImportSessionQuery(TenantA, session.Id, Page: 1, Limit: 2);
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // 3. Assertions
        result.ImportSessionId.Should().Be(session.Id);
        result.SourceFileName.Should().Be("test.xlsx");
        result.Rows.TotalCount.Should().Be(3);
        result.Rows.PageNumber.Should().Be(1);
        result.Rows.PageSize.Should().Be(2);
        result.Rows.Items.Count.Should().Be(2);

        result.Rows.Items[0].RowNumber.Should().Be(1);
        result.Rows.Items[0].SkuCode.Should().Be("SKU-001");
        result.Rows.Items[0].IsValid.Should().BeTrue();

        result.Rows.Items[1].RowNumber.Should().Be(2);
        result.Rows.Items[1].SkuCode.Should().Be("SKU-002");
        result.Rows.Items[1].IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenRequestingDifferentPages_ReturnsCorrectSubset()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var session = SkuImportSession.Create(TenantA, "test.xlsx");
        for (int i = 1; i <= 5; i++)
        {
            session.AddRow(i, $"PROD-00{i}", Guid.NewGuid(), $"SKU-00{i}", $"Name {i}", null, null, 10m * i, true, null, null);
        }
        db.Set<SkuImportSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetSkuImportSessionQueryHandler(CreateUnitOfWork(db));

        // Query page 2, limit 2 (should skip 2, take 2 -> rows 3 and 4)
        var query = new GetSkuImportSessionQuery(TenantA, session.Id, Page: 2, Limit: 2);
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Rows.TotalCount.Should().Be(5);
        result.Rows.Items.Count.Should().Be(2);
        result.Rows.Items[0].RowNumber.Should().Be(3);
        result.Rows.Items[0].SkuCode.Should().Be("SKU-003");
        result.Rows.Items[1].RowNumber.Should().Be(4);
        result.Rows.Items[1].SkuCode.Should().Be("SKU-004");
    }

    [Fact]
    public async Task Handle_WhenSessionDoesNotExist_ThrowsNotFoundException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = new GetSkuImportSessionQueryHandler(CreateUnitOfWork(db));
        var query = new GetSkuImportSessionQuery(TenantA, Guid.NewGuid(), Page: 1, Limit: 10);

        var act = () => handler.Handle(query, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "IMPORT_SESSION_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WhenSessionBelongsToOtherTenant_ThrowsNotFoundException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var session = SkuImportSession.Create(TenantB, "test.xlsx");
        db.Set<SkuImportSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetSkuImportSessionQueryHandler(CreateUnitOfWork(db));
        var query = new GetSkuImportSessionQuery(TenantA, session.Id, Page: 1, Limit: 10);

        var act = () => handler.Handle(query, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "IMPORT_SESSION_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WhenRowsAreDeleted_ExcludesDeletedRows()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var session = SkuImportSession.Create(TenantA, "test.xlsx");
        session.AddRow(1, "PROD-001", Guid.NewGuid(), "SKU-001", "Name 1", null, null, 10m, true, null, null);
        session.AddRow(2, "PROD-002", Guid.NewGuid(), "SKU-002", "Name 2", null, null, 20m, true, null, null);
        db.Set<SkuImportSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Soft-delete the first row
        session.Rows.First().MarkDeleted();
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetSkuImportSessionQueryHandler(CreateUnitOfWork(db));
        var query = new GetSkuImportSessionQuery(TenantA, session.Id, Page: 1, Limit: 10);
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Rows.TotalCount.Should().Be(1);
        result.Rows.Items.Count.Should().Be(1);
        result.Rows.Items[0].RowNumber.Should().Be(2);
        result.Rows.Items[0].SkuCode.Should().Be("SKU-002");
    }
}
