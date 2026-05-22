# Specification Pattern

Thư mục này chứa hạ tầng query dùng cho Application layer. 

Mục tiêu: gom điều kiện truy vấn (`Where`, `Include`, `OrderBy`, `Skip`, `Take`) vào một object riêng, để handler/repository không phải lặp logic filter.

## Thành phần

```text
ISpecification.cs          Contract mô tả query shape
Specification.cs           Base class lưu criteria/include/sort/paging
ExpressionExtensions.cs    Gộp expression bằng AND/OR
SpecificationEvaluator.cs  Áp specification vào IQueryable
```

## Cách hoạt động

Một specification chứa các phần sau:

```text
Criteria             WHERE condition
Includes             navigation cần eager load
OrderBy              sort tăng dần
OrderByDescending    sort giảm dần
Skip / Take          paging
IsPagingEnabled      bật/tắt paging
```

`Specification<T>` cung cấp các method `protected` cho specification cụ thể:

```csharp
AddCriteria(x => ...);
AddInclude(x => ...);
ApplyOrderBy(x => ...);
ApplyOrderByDescending(x => ...);
ApplyPaging(skip, take);
```

`AddCriteria` có thể gọi nhiều lần. Lần đầu set `Criteria`, các lần sau gộp thêm bằng `AND`:

```text
AddCriteria(A)  => Criteria = A
AddCriteria(B)  => Criteria = A AND B
AddCriteria(C)  => Criteria = A AND B AND C
```

Việc gộp expression dùng `ExpressionExtensions.And()`. Helper này thay parameter của các lambda khác nhau về cùng một parameter rồi tạo expression mới.

Ví dụ:

```csharp
x => x.TenantId == tenantId
```

và:

```csharp
y => y.CategoryId == categoryId
```

được gộp thành:

```csharp
entity => entity.TenantId == tenantId && entity.CategoryId == categoryId
```

## SpecificationEvaluator

`SpecificationEvaluator.GetQuery(...)` nhận `IQueryable<T>` và `ISpecification<T>`, sau đó apply theo thứ tự:

```text
1. Criteria   => Where(...)
2. Includes   => Include(...)
3. Order      => OrderBy(...) / OrderByDescending(...)
4. Paging     => Skip(...).Take(...)
```

Có thể tắt paging khi cần count tổng:

```csharp
var countQuery = SpecificationEvaluator.GetQuery(query, specification, applyPaging: false);
var totalCount = await countQuery.CountAsync(ct);
```

Lấy data trang hiện tại thì dùng mặc định `applyPaging: true`:

```csharp
var items = await SpecificationEvaluator.GetQuery(query, specification)
    .ToListAsync(ct);
```

## Ví dụ: SearchSkusSpecification

`SearchSkusSpecification` nhận input từ use case search SKU:

```csharp
public SearchSkusSpecification(
    Guid tenantId,
    string? search,
    Guid? categoryId,
    int page,
    int limit)
```

Nó build query như sau:

```csharp
AddCriteria(x => x.TenantId == tenantId && x.DeletedAt == null);

if (categoryId.HasValue)
{
    AddCriteria(x => x.CategoryId == categoryId.Value);
}

if (!string.IsNullOrWhiteSpace(search))
{
    var keyword = search.Trim().ToLower();

    Expression<Func<SkuEntity, bool>> searchCriteria = x =>
        x.SkuCode.ToLower().Contains(keyword) ||
        x.Name.ToLower().Contains(keyword) ||
        (x.Description != null && x.Description.ToLower().Contains(keyword));

    AddCriteria(searchCriteria);
}

ApplyOrderByDescending(x => x.UpdatedAt);
ApplyPaging((page - 1) * limit, limit);
```

Nếu request là:

```http
GET /api/v1/skus?search=milk&categoryId=<guid>&page=2&limit=20
```

Specification cuối cùng tương đương:

```text
WHERE TenantId = current tenant
  AND DeletedAt IS NULL
  AND CategoryId = <guid>
  AND (
      LOWER(SkuCode) LIKE '%milk%'
      OR LOWER(Name) LIKE '%milk%'
      OR (Description IS NOT NULL AND LOWER(Description) LIKE '%milk%')
  )
ORDER BY UpdatedAt DESC
OFFSET 20
LIMIT 20
```

Trong handler, query thường dùng 2 lần:

```csharp
var query = uow.Repository<SkuEntity>().Query().AsNoTracking();

var countQuery = SpecificationEvaluator.GetQuery(query, specification, false);
var totalCount = await countQuery.CountAsync(ct);

var items = await SpecificationEvaluator.GetQuery(query, specification)
    .Select(x => new SearchSkusResponse(...))
    .ToListAsync(ct);
```

## Cách áp dụng cho use case mới

Khi cần query danh sách có filter/sort/paging, tạo specification riêng trong folder use case.

Ví dụ:

```text
WMS.Application/<Module>/<Feature>/Queries/<UseCase>/
  <UseCase>Query.cs
  <UseCase>QueryHandler.cs
  <UseCase>Specification.cs
```

Checklist:

1. Tạo class kế thừa `Specification<TEntity>`.
2. Nhận input cần filter qua constructor.
3. Luôn thêm tenant filter nếu entity là tenant-scoped.
4. Luôn thêm soft-delete filter nếu use case không cần dữ liệu đã xóa.
5. Thêm optional filter bằng `if`.
6. Thêm search keyword nếu cần.
7. Thêm sort rõ ràng để paging ổn định.
8. Thêm paging bằng `ApplyPaging((page - 1) * limit, limit)`.
9. Trong handler, normalize `page` và `limit` trước khi tạo specification.
10. Dùng `SpecificationEvaluator.GetQuery(..., false)` cho count tổng.
11. Dùng `SpecificationEvaluator.GetQuery(...)` cho items.
12. Project sang DTO bằng `.Select(...)`, không trả entity trực tiếp.

Template:

```csharp
public sealed class ExampleSpecification : Specification<ExampleEntity>
{
    public ExampleSpecification(
        Guid tenantId,
        string? search,
        int page,
        int limit)
    {
        AddCriteria(x => x.TenantId == tenantId && x.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            AddCriteria(x => x.Name.ToLower().Contains(keyword));
        }

        ApplyOrderByDescending(x => x.UpdatedAt);
        ApplyPaging((page - 1) * limit, limit);
    }
}
```

Handler template:

```csharp
var page = Math.Max(request.Page, PaginationDefaults.Page);
var limit = Math.Clamp(request.Limit, PaginationDefaults.MinLimit, PaginationDefaults.MaxLimit);

var specification = new ExampleSpecification(request.TenantId, request.Search, page, limit);
var query = uow.Repository<ExampleEntity>().Query().AsNoTracking();

var totalCount = await SpecificationEvaluator
    .GetQuery(query, specification, false)
    .CountAsync(ct);

var items = await SpecificationEvaluator
    .GetQuery(query, specification)
    .Select(x => new ExampleResponse(...))
    .ToListAsync(ct);

return new PagedResult<ExampleResponse>
{
    Items = items,
    TotalCount = totalCount,
    PageNumber = page,
    PageSize = limit
};
```

## Lưu ý

- Không đặt business write logic trong specification. Specification chỉ mô tả query.
- Không gọi `ToListAsync`, `CountAsync`, hoặc execute query trong specification.
- Không trả `IQueryable` ra khỏi Application boundary.
- Search bằng `ToLower().Contains(...)` dễ dùng nhưng có thể khó tận dụng index. Với PostgreSQL, cân nhắc `EF.Functions.ILike` nếu cần tối ưu.
- Nếu cần `CategoryName` hoặc field từ navigation, phải cấu hình navigation trong EF hoặc join rõ ràng. `AddInclude` không có tác dụng nếu navigation bị `Ignore` trong configuration.
