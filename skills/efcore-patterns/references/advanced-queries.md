# Advanced EF Core Queries

---

## Projection Best Practices

Always project to a DTO/anonymous type as early as possible — never load full entities for read-only views:

```csharp
// ✅ Project in the DB query — only fetches needed columns
var summaries = await context.Orders
    .AsNoTracking()
    .Where(o => o.CustomerId == customerId)
    .Select(o => new OrderSummaryDto(
        o.Id,
        o.CreatedAt,
        o.Status.ToString(),
        o.Lines.Sum(l => l.Quantity * l.UnitPrice)))
    .OrderByDescending(o => o.CreatedAt)
    .ToListAsync(ct);

// ❌ Loads full entities + navigation properties, then projects in memory
var orders = await context.Orders
    .Include(o => o.Lines)
    .ToListAsync(ct);
var summaries = orders.Select(o => new OrderSummaryDto(...));
```

---

## Split Queries (Avoid Cartesian Explosion)

When including multiple collection navigations, use `AsSplitQuery()`:

```csharp
// Without split query — SQL JOIN produces duplicate rows for each combination
// 10 orders × 5 lines × 3 tags = 150 rows returned

// ✅ Split query — 3 separate SQL SELECTs, no duplication
var orders = await context.Orders
    .Include(o => o.Lines).ThenInclude(l => l.Product)
    .Include(o => o.Tags)
    .AsSplitQuery()
    .AsNoTracking()
    .ToListAsync(ct);

// Configure globally (careful — may use more roundtrips)
options.UseSqlServer(connStr, sql =>
    sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
```

---

## Pagination

```csharp
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;
}

public async Task<PagedResult<ProductDto>> GetPagedAsync(
    int page, int pageSize, CancellationToken ct)
{
    var query = context.Products.AsNoTracking().Where(p => p.IsActive);

    var total = await query.CountAsync(ct);
    var items = await query
        .OrderBy(p => p.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(p => new ProductDto(p.Id, p.Name, p.Price))
        .ToListAsync(ct);

    return new PagedResult<ProductDto>(items, total, page, pageSize);
}
```

---

## JSON Column Queries (.NET 8+, SQL Server 2016+)

```csharp
// Entity with JSON-mapped owned type
public class Order
{
    public int Id { get; set; }
    public OrderMetadata Metadata { get; set; } = null!;
}

public class OrderMetadata
{
    public string Source   { get; set; } = string.Empty;
    public string[]? Tags  { get; set; }
    public Dictionary<string, string>? Extra { get; set; }
}

// Configuration
builder.Entity<Order>().OwnsOne(o => o.Metadata, meta =>
    meta.ToJson());  // Stored as JSON column

// Query JSON properties
var webOrders = await context.Orders
    .Where(o => o.Metadata.Source == "web")
    .AsNoTracking()
    .ToListAsync(ct);

// Query JSON array elements (EF Core 8+)
var taggedOrders = await context.Orders
    .Where(o => o.Metadata.Tags!.Contains("priority"))
    .ToListAsync(ct);
```

---

## Full-Text Search (SQL Server)

```csharp
// Configuration
builder.Entity<Product>().Property(p => p.Description)
    .HasAnnotation("FullText", true);

// EF.Functions.Contains for full-text search
var results = await context.Products
    .Where(p => EF.Functions.Contains(p.Description, "\"organic\" AND \"fresh\""))
    .AsNoTracking()
    .ToListAsync(ct);

// FreeText (more flexible, less precise)
var results2 = await context.Products
    .Where(p => EF.Functions.FreeText(p.Description, "healthy organic"))
    .ToListAsync(ct);
```

---

## Global Query Filters

Applied automatically to **every** query for that entity type. Ideal for soft delete and multi-tenancy:

```csharp
// In OnModelCreating
modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
modelBuilder.Entity<Order>()
    .HasQueryFilter(o => o.TenantId == _tenantService.CurrentTenantId);

// Bypass the filter when needed
var allProducts = await context.Products
    .IgnoreQueryFilters()  // Includes soft-deleted records
    .ToListAsync(ct);
```

---

## Value Converters

Map .NET types to database column types:

```csharp
// Built-in: enum → string
builder.Property(p => p.Status)
    .HasConversion<string>()
    .HasMaxLength(20);

// Custom: Money value object → decimal column
builder.Property(p => p.Price)
    .HasConversion(
        price => price.Amount,
        amount => new Money(amount, Currency.USD));

// List<string> → pipe-delimited string
builder.Property(p => p.Tags)
    .HasConversion(
        tags => string.Join('|', tags),
        str  => str.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList());
```

---

## Compiled Queries — Full Reference

```csharp
public static class CompiledQueries
{
    // Async compiled query
    public static readonly Func<AppDbContext, int, Task<Product?>> GetById =
        EF.CompileAsyncQuery((AppDbContext ctx, int id) =>
            ctx.Products.FirstOrDefault(p => p.Id == id));

    // With multiple parameters
    public static readonly Func<AppDbContext, int, decimal, IAsyncEnumerable<Product>>
        GetByCategory = EF.CompileAsyncQuery(
            (AppDbContext ctx, int categoryId, decimal maxPrice) =>
                ctx.Products
                   .Where(p => p.CategoryId == categoryId && p.Price <= maxPrice)
                   .OrderBy(p => p.Price));
}

// Usage
var product  = await CompiledQueries.GetById(context, 42);
var products = CompiledQueries.GetByCategory(context, 1, 100m);
await foreach (var p in products) { /* stream */ }
```

---

## Learn More

| Topic | Query |
|-------|-------|
| JSON columns | `microsoft_docs_search(query="EF Core JSON columns owned entity ToJson .NET 8")` |
| Global query filters | `microsoft_docs_search(query="EF Core global query filters HasQueryFilter")` |
| Value converters | `microsoft_docs_search(query="EF Core value converters custom type mapping")` |
| Compiled queries | `microsoft_docs_search(query="EF Core compiled queries EF.CompileAsyncQuery performance")` |
| Full-text search | `microsoft_docs_search(query="EF Core full text search SQL Server Contains FreeText")` |
