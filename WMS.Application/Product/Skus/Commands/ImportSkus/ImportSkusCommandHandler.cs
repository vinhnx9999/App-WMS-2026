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

        ImportSkuMasterData masterData;
        if (!validationResult.HasErrors)
        {
            masterData = await ValidateAgainstDatabaseAsync(request.TenantId, validationResult.Rows, request.AutoCreateMasterData, errors, ct);
        }
        else
        {
            masterData = new ImportSkuMasterData([], [], [], [], [], []);
        }

        if (errors.Count > 0)
        {
            return new ImportSkusResponse(
                totalRows,
                0,
                errors.Select(x => x.RowNumber).Distinct().Count(),
                errors);
        }

        if (request.Mode == ImportSkuMode.ValidateOnly)
        {
            return new ImportSkusResponse(totalRows, 0, 0, []);
        }

        var insertedRows = await InsertAsync(request.TenantId, validationResult.Rows, masterData, ct);
        return new ImportSkusResponse(totalRows, insertedRows, 0, []);
    }

    private async Task<ImportSkuMasterData> ValidateAgainstDatabaseAsync(
        Guid tenantId,
        IReadOnlyList<ImportSkuRowInput> rows,
        bool autoCreateMasterData,
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
            name => new Category { TenantId = tenantId, Name = name },
            autoCreateMasterData,
            ct);

        var specifications = await ResolveMasterDataAsync<SkuAttribute>(
            tenantId,
            rows.Select(x => x.SpecificationCode),
            x => x.Code,
            code => new SkuAttribute { TenantId = tenantId, Code = code },
            autoCreateMasterData,
            ct);

        var unitOfMeasures = await ResolveMasterDataAsync<UnitOfMeasure>(
            tenantId,
            rows.Select(x => x.UnitOfMeasureCode),
            x => x.Code,
            code => new UnitOfMeasure { TenantId = tenantId, Code = code },
            autoCreateMasterData,
            ct);

        if (!autoCreateMasterData)
        {
            AddMissingMasterDataErrors(rows, categories.Entities, x => x.CategoryName, nameof(ImportSkuRowInput.CategoryName), "CATEGORY_NOT_FOUND", "Category not found", errors);
            AddMissingMasterDataErrors(rows, specifications.Entities, x => x.SpecificationCode, nameof(ImportSkuRowInput.SpecificationCode), "SPECIFICATION_NOT_FOUND", "Specification not found", errors);
            AddMissingMasterDataErrors(rows, unitOfMeasures.Entities, x => x.UnitOfMeasureCode, nameof(ImportSkuRowInput.UnitOfMeasureCode), "UNIT_OF_MEASURE_NOT_FOUND", "Unit of measure not found", errors);
        }

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
        bool autoCreateMasterData,
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

        if (!autoCreateMasterData)
        {
            return new ResolvedMasterData<TEntity>(entitiesByValue, entitiesToCreate);
        }

        foreach (var value in normalizedValues.Where(x => !entitiesByValue.ContainsKey(x)))
        {
            var entity = createEntity(value);
            entitiesByValue.Add(value, entity);
            entitiesToCreate.Add(entity);
        }

        return new ResolvedMasterData<TEntity>(entitiesByValue, entitiesToCreate);
    }

    private async Task<int> InsertAsync(
        Guid tenantId,
        IReadOnlyList<ImportSkuRowInput> rows,
        ImportSkuMasterData masterData,
        CancellationToken ct)
    {
        await uow.BeginTransactionAsync(ct);

        try
        {
            await AddNewMasterDataAsync(masterData.CategoriesToCreate, ct);
            await AddNewMasterDataAsync(masterData.SpecificationsToCreate, ct);
            await AddNewMasterDataAsync(masterData.UnitOfMeasuresToCreate, ct);
            await uow.SaveChangesAsync(ct);

            var skus = rows.Select(row => CreateSku(tenantId, row, masterData)).ToList();

            foreach (var batch in skus.Chunk(SkuImportDefaults.BatchSize))
            {
                await uow.Repository<Sku>().AddRangeAsync(batch, ct);
                await uow.SaveChangesAsync(ct);
            }

            await uow.CommitAsync(ct);
            return skus.Count;
        }
        catch
        {
            await uow.RollbackAsync(ct);
            throw;
        }
    }

    private async Task AddNewMasterDataAsync<TEntity>(IReadOnlyCollection<TEntity> entities, CancellationToken ct)
        where TEntity : BaseEntity
    {
        if (entities.Count > 0)
        {
            await uow.Repository<TEntity>().AddRangeAsync(entities, ct);
        }
    }

    private static Sku CreateSku(Guid tenantId, ImportSkuRowInput row, ImportSkuMasterData masterData)
    {
        var sku = new Sku
        {
            TenantId = tenantId,
            CategoryId = row.CategoryName is not null ? masterData.Categories[row.CategoryName].Id : null,
            SkuCode = row.SkuCode!,
            Name = row.SkuName,
            GoodsNature = row.GoodsNature,
            Description = null,
            ReferencePrice = null
        };

        if (row.SpecificationCode is not null)
        {
            sku.SkuSpecifications.Add(new SkuAttributeValue
            {
                TenantId = tenantId,
                SkuId = sku.Id,
                Sku = sku,
                SpecificationId = masterData.Specifications[row.SpecificationCode].Id,
                Specification = masterData.Specifications[row.SpecificationCode]
            });
        }

        if (row.UnitOfMeasureCode is not null)
        {
            sku.SkuUnitOfMeasures.Add(new SkuUnitOfMeasure
            {
                TenantId = tenantId,
                SkuId = sku.Id,
                Sku = sku,
                UnitOfMeasureId = masterData.UnitOfMeasures[row.UnitOfMeasureCode].Id,
                UnitOfMeasure = masterData.UnitOfMeasures[row.UnitOfMeasureCode],
                ConversionFactor = row.ConversionFactor!.Value
            });
        }

        return sku;
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
