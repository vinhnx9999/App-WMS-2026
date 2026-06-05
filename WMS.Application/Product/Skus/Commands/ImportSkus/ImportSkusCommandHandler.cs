using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Product.Skus.DTOs;
using WMS.Domain.Common;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;

namespace WMS.Application.Product.Skus.Commands.ImportSkus;

public sealed class ImportSkusCommandHandler(IUnitOfWork uow) : IRequestHandler<ImportSkusCommand, ImportSkusResponse>
{
    public async Task<ImportSkusResponse> Handle(ImportSkusCommand request, CancellationToken ct)
    {
        var validationResult = ImportSkuRowValidator.Validate(request.Rows);
        var errors = validationResult.Errors.ToList();
        var totalRows = request.Rows.Count;

        var candidateRows = RowsWithoutErrors(validationResult.Rows, errors);
        var masterData = candidateRows.Count == 0
            ? new ImportSkuMasterData([], [], [], [], [], [])
            : await ValidateAgainstDatabaseAsync(request.TenantId, candidateRows, errors, ct);

        var rowsToInsert = RowsWithoutErrors(candidateRows, errors);

        if (rowsToInsert.Count == 0)
        {
            return new ImportSkusResponse(
                totalRows,
                0,
                errors.Select(x => x.RowNumber).Distinct().Count(),
                errors);
        }

        var (insertedRows, _) = await InsertAsync(request.TenantId, rowsToInsert, masterData, errors, ct);

        return new ImportSkusResponse(
            totalRows,
            insertedRows,
            errors.Select(x => x.RowNumber).Distinct().Count(),
            errors);
    }

    private static IReadOnlyList<ImportSkuRowInput> RowsWithoutErrors(
        IReadOnlyList<ImportSkuRowInput> rows,
        IReadOnlyList<ImportSkuRowErrorResponse> errors)
    {
        var failedRowNumbers = errors.Select(x => x.RowNumber).ToHashSet();
        return rows.Where(x => !failedRowNumbers.Contains(x.RowNumber)).ToList();
    }

    private async Task<ImportSkuMasterData> ValidateAgainstDatabaseAsync(
        Guid tenantId,
        IReadOnlyList<ImportSkuRowInput> rows,
        List<ImportSkuRowErrorResponse> errors,
        CancellationToken ct)
    {
        var skuCodes = NormalizeValues(rows.Select(x => x.SkuCode));

        var existingSkuCodes = await uow.Repository<Sku>().Query()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted && skuCodes.Contains(x.SkuCode.ToUpper()))
            .Select(x => x.SkuCode)
            .ToListAsync(ct);

        var existingSkuCodeSet = existingSkuCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows.Where(x => x.SkuCode is not null && existingSkuCodeSet.Contains(x.SkuCode)))
        {
            errors.Add(Error(row, nameof(row.SkuCode), "SKU_CODE_EXISTS", "SKU code already exists"));
        }

        var categories = await ResolveMasterDataAsync<Category>(
            tenantId,
            rows.Select(x => x.CategoryName),
            x => x.Name,
            name => Category.Create(tenantId, name),
            ct);

        var specifications = await ResolveMasterDataAsync<SkuAttribute>(
            tenantId,
            rows.Select(x => x.SpecificationCode),
            x => x.Code,
            code => SkuAttribute.Create(tenantId, code, code),
            ct);

        var unitOfMeasures = await ResolveMasterDataAsync<UnitOfMeasure>(
            tenantId,
            rows.Select(x => x.UnitOfMeasureCode),
            x => x.Code,
            code => UnitOfMeasure.Create(tenantId, code, code, null),
            ct);

        return new ImportSkuMasterData(
            categories.Entities,
            specifications.Entities,
            unitOfMeasures.Entities,
            categories.EntitiesToCreate,
            specifications.EntitiesToCreate,
            unitOfMeasures.EntitiesToCreate);
    }

    private async Task<ResolvedMasterData<TEntity>> ResolveMasterDataAsync<TEntity>(
        Guid tenantId,
        IEnumerable<string?> values,
        System.Linq.Expressions.Expression<Func<TEntity, string>> field,
        Func<string, TEntity> createEntity,
        CancellationToken ct)
        where TEntity : BaseEntity
    {
        var normalizedValues = values
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedValues.Count == 0)
        {
            return new ResolvedMasterData<TEntity>(new Dictionary<string, TEntity>(StringComparer.OrdinalIgnoreCase), []);
        }

        var normalizedLookupValues = NormalizeValues(normalizedValues);
        var existingEntities = await uow.Repository<TEntity>().Query()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted && normalizedLookupValues.Contains(EF.Property<string>(x, GetPropertyName(field)).ToUpper()))
            .ToListAsync(ct);

        var entitiesByValue = existingEntities.ToDictionary(field.Compile(), StringComparer.OrdinalIgnoreCase);
        var entitiesToCreate = new List<TEntity>();

        foreach (var value in normalizedValues.Where(x => !entitiesByValue.ContainsKey(x)))
        {
            var entity = createEntity(value);
            entitiesByValue.Add(value, entity);
            entitiesToCreate.Add(entity);
        }

        return new ResolvedMasterData<TEntity>(entitiesByValue, entitiesToCreate);
    }

    private async Task<(int insertedRows, int failedRows)> InsertAsync(
        Guid tenantId,
        IReadOnlyList<ImportSkuRowInput> rows,
        ImportSkuMasterData masterData,
        List<ImportSkuRowErrorResponse> errors,
        CancellationToken ct)
    {
        // First, save any new master data in a single initial transaction
        if (masterData.CategoriesToCreate.Count > 0 || masterData.SpecificationsToCreate.Count > 0 || masterData.UnitOfMeasuresToCreate.Count > 0)
        {
            await uow.BeginTransactionAsync(ct);
            try
            {
                await AddNewMasterDataAsync(masterData.CategoriesToCreate, ct);
                await AddNewMasterDataAsync(masterData.SpecificationsToCreate, ct);
                await AddNewMasterDataAsync(masterData.UnitOfMeasuresToCreate, ct);
                await uow.SaveChangesAsync(ct);
                await uow.CommitAsync(ct);
            }
            catch
            {
                await uow.RollbackAsync(ct);
                throw;
            }
        }

        int insertedRows = 0;
        int failedRows = 0;

        var groupedRows = rows.GroupBy(x => x.ProductCode!.Trim().ToUpperInvariant()).ToList();

        foreach (var group in groupedRows)
        {
            await uow.BeginTransactionAsync(ct);
            try
            {
                var productCode = group.Key;

                var product = await uow.Repository<Domain.Entities.Product.Product>().Query()
                    .Include(x => x.Skus)
                    .FirstOrDefaultAsync(x => x.TenantId == tenantId && !x.IsDeleted && x.ProductCode.ToUpper() == productCode, ct);

                if (product is null)
                {
                    // Find category id if available from the first row in the group
                    var firstCategoryName = group.FirstOrDefault(x => x.CategoryName is not null)?.CategoryName;
                    Guid? categoryId = firstCategoryName is null ? null : masterData.Categories[firstCategoryName].Id;

                    product = Domain.Entities.Product.Product.Create(tenantId, group.First().ProductCode!, $"{group.First().ProductCode} Product", categoryId: categoryId);
                    await uow.Repository<Domain.Entities.Product.Product>().AddAsync(product, ct);
                }

                foreach (var batch in group.Chunk(SkuImportDefaults.BatchSize))
                {
                    foreach (var row in batch)
                    {
                        var sku = product.AddSku(
                            tenantId: tenantId,
                            skuCode: row.SkuCode!,
                            name: row.SkuName,
                            goodsNature: row.GoodsNature,
                            description: null,
                            referencePrice: null);

                        if (row.SpecificationCode is not null)
                        {
                            var specId = masterData.Specifications[row.SpecificationCode].Id;
                            sku.AddAttribute(specId, row.SpecificationCode);
                        }

                        if (row.UnitOfMeasureCode is not null)
                        {
                            var uomId = masterData.UnitOfMeasures[row.UnitOfMeasureCode].Id;
                            product.AllowSkuUnitOfMeasure(sku.Id, uomId, updatedBy: null);
                        }
                    }
                }

                await uow.SaveChangesAsync(ct);
                await uow.CommitAsync(ct);
                insertedRows += group.Count();
            }
            catch (OperationCanceledException)
            {
                await uow.RollbackAsync(ct);
                throw;
            }
            catch (Exception ex)
            {
                await uow.RollbackAsync(ct);
                failedRows += group.Count();

                // Add errors for all rows in the failed group
                foreach (var row in group)
                {
                    errors.Add(Error(row, "ProductGroup", "PRODUCT_GROUP_FAILED", $"Failed to import product group: {ex.Message}"));
                }
            }
        }

        return (insertedRows, failedRows);
    }

    private async Task AddNewMasterDataAsync<TEntity>(IReadOnlyCollection<TEntity> entities, CancellationToken ct)
        where TEntity : BaseEntity
    {
        if (entities.Count > 0)
        {
            await uow.Repository<TEntity>().AddRangeAsync(entities, ct);
        }
    }

    private static void AddMissingMasterDataErrors<TEntity>(
        IReadOnlyList<ImportSkuRowInput> rows,
        IReadOnlyDictionary<string, TEntity> entities,
        Func<ImportSkuRowInput, string?> getValue,
        string field,
        string code,
        string message,
        List<ImportSkuRowErrorResponse> errors)
    {
        foreach (var row in rows.Where(x => getValue(x) is not null && !entities.ContainsKey(getValue(x)!)))
        {
            errors.Add(Error(row, field, code, message));
        }
    }

    private static string GetPropertyName<TEntity>(System.Linq.Expressions.Expression<Func<TEntity, string>> field)
    {
        return field.Body is System.Linq.Expressions.MemberExpression memberExpression
            ? memberExpression.Member.Name
            : throw new InvalidOperationException("Master data field must be a property access");
    }

    private static List<string> NormalizeValues(IEnumerable<string?> values)
    {
        return values
            .OfType<string>()
            .Select(x => x.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static ImportSkuRowErrorResponse Error(
        ImportSkuRowInput row,
        string field,
        string code,
        string message)
    {
        return new ImportSkuRowErrorResponse(row.RowNumber, row.SkuCode, field, code, message);
    }

    private sealed record ImportSkuMasterData(
        Dictionary<string, Category> Categories,
        Dictionary<string, SkuAttribute> Specifications,
        Dictionary<string, UnitOfMeasure> UnitOfMeasures,
        IReadOnlyCollection<Category> CategoriesToCreate,
        IReadOnlyCollection<SkuAttribute> SpecificationsToCreate,
        IReadOnlyCollection<UnitOfMeasure> UnitOfMeasuresToCreate);

    private sealed record ResolvedMasterData<TEntity>(
        Dictionary<string, TEntity> Entities,
        IReadOnlyCollection<TEntity> EntitiesToCreate);
}
