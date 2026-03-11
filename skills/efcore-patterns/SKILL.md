---
name: efcore-patterns
description: Entity Framework Core patterns and best practices for .NET 8/9/10. Use when configuring DbContext, writing entity type configurations (IEntityTypeConfiguration), EF Core migrations, LINQ queries, eager loading (Include/ThenInclude), no-tracking queries (AsNoTracking), global query filters, bulk operations (ExecuteUpdateAsync/ExecuteDeleteAsync), compiled queries, owned entities, value converters, table splitting, TPH/TPT/TPC inheritance strategies, EF Core interceptors, SaveChanges interceptors, optimistic concurrency (RowVersion/ConcurrencyToken), connection resiliency, raw SQL (FromSql/ExecuteSql), multi-tenancy, repository pattern with EF Core, or unit of work pattern.
license: Complete terms in LICENSE.txt
---

# Entity Framework Core Patterns

Production-ready EF Core patterns for .NET 8/9/10 covering configuration, querying, performance, and lifecycle management.

## When to Use This Skill

- Configuring `DbContext` and entity mappings
- Writing `IEntityTypeConfiguration<T>` classes
- Running and managing migrations
- Writing efficient LINQ queries (avoid N+1, use projections)
- Implementing soft delete with global query filters
- Using bulk operations with `ExecuteUpdateAsync`/`ExecuteDeleteAsync`
- Adding EF Core interceptors (audit logging, soft delete, multi-tenancy)
- Handling optimistic concurrency conflicts
- Setting up connection resiliency for production SQL Server/PostgreSQL

## Prerequisites

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer   # or .Npgsql
dotnet add package Microsoft.EntityFrameworkCore.Design      # migrations tooling
dotnet tool install -g dotnet-ef
```

## DbContext & Registration

```csharp
// AppDbContext.cs
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product>  Products  => Set<Product>();
    public DbSet<Order>    Orders    => Set<Order>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        // Auto-discovers all IEntityTypeConfiguration<T> in this assembly
        model.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    // Audit automation — fires on every SaveChanges
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var e in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (e.State == EntityState.Added)   e.Entity.CreatedAt = DateTime.UtcNow;
            if (e.State is EntityState.Added
                        or EntityState.Modified) e.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(ct);
    }
}

// Registration (Program.cs)
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure(
            maxRetryCount:  5,
            maxRetryDelay:  TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null))
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution));
```

## Entity Configuration Pattern

```csharp
// Configurations/ProductConfiguration.cs — one file per entity
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("Products");
        b.HasKey(p => p.Id);

        b.Property(p => p.Name).IsRequired().HasMaxLength(100);
        b.Property(p => p.Price).HasColumnType("decimal(18,2)");
        b.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        // Owned entity (stored in same table — no FK)
        b.OwnsOne(p => p.Address, a =>
        {
            a.Property(x => x.Street).HasMaxLength(200);
            a.Property(x => x.City).HasMaxLength(100);
        });

        // Optimistic concurrency
        b.Property(p => p.RowVersion).IsRowVersion();

        // Unique index
        b.HasIndex(p => p.Sku).IsUnique();

        // Relationship
        b.HasOne(p => p.Category)
         .WithMany(c => c.Products)
         .HasForeignKey(p => p.CategoryId)
         .OnDelete(DeleteBehavior.Restrict);

        // Global query filter — soft delete
        b.HasQueryFilter(p => !p.IsDeleted);
    }
}
```

## Query Patterns

```csharp
// ── Read-only projection — fastest, no tracking, only fetch needed columns
var products = await context.Products
    .AsNoTracking()
    .Where(p => p.CategoryId == id && p.Price <= maxPrice)
    .OrderBy(p => p.Name)
    .Select(p => new ProductDto(p.Id, p.Name, p.Price))   // project early
    .ToListAsync(ct);

// ── Eager loading with split queries (avoids cartesian explosion on collections)
var orders = await context.Orders
    .Include(o => o.Customer)
    .Include(o => o.Lines)
        .ThenInclude(l => l.Product)
    .AsSplitQuery()          // separate SQL per collection Include
    .AsNoTracking()
    .ToListAsync(ct);

// ── Compiled query — eliminates query translation overhead on hot paths
private static readonly Func<AppDbContext, int, Task<Product?>> _getById =
    EF.CompileAsyncQuery((AppDbContext db, int id) =>
        db.Products.FirstOrDefault(p => p.Id == id));

var product = await _getById(context, productId);

// ── Bulk update (EF Core 7+) — single UPDATE statement, no entity load
await context.Products
    .Where(p => p.CategoryId == oldId)
    .ExecuteUpdateAsync(s => s
        .SetProperty(p => p.CategoryId, newId)
        .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);

// ── Bulk delete (EF Core 7+)
await context.Products
    .Where(p => p.IsDeleted && p.DeletedAt < DateTime.UtcNow.AddDays(-90))
    .ExecuteDeleteAsync(ct);

// ── Raw SQL (when LINQ isn't enough)
var results = await context.Products
    .FromSql($"EXEC sp_GetActiveProducts @CategoryId = {categoryId}")
    .AsNoTracking()
    .ToListAsync(ct);
```

## Soft Delete Interceptor

```csharp
public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData data, InterceptionResult<int> result)
    {
        if (data.Context is null) return result;

        foreach (var entry in data.Context.ChangeTracker
            .Entries<ISoftDeletable>()
            .Where(e => e.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted  = true;
            entry.Entity.DeletedAt  = DateTime.UtcNow;
        }
        return result;
    }
}

// Registration
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(connStr)
     .AddInterceptors(new SoftDeleteInterceptor()));
```

## Migrations

```bash
# Development
dotnet ef migrations add AddProductTable \
  --project src/Infrastructure \
  --startup-project src/Api

# Production — generate idempotent SQL script
dotnet ef migrations script --idempotent --output deploy/migrations.sql \
  --project src/Infrastructure --startup-project src/Api

# Apply at runtime (small apps / dev)
await context.Database.MigrateAsync(ct);
```

```csharp
// Migrator IHostedService — apply on startup in production
public class DatabaseMigrator(IServiceProvider sp, ILogger<DatabaseMigrator> log)
    : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope   = sp.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        log.LogInformation("Applying EF Core migrations…");
        await context.Database.MigrateAsync(ct);
        log.LogInformation("Migrations applied.");
    }
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

// Register
builder.Services.AddHostedService<DatabaseMigrator>();
```

See [advanced-queries.md](references/advanced-queries.md) for JSON columns, full-text, spatial, and split queries.
See [inheritance-strategies.md](references/inheritance-strategies.md) for TPH, TPT, and TPC mapping.
See [multi-tenancy.md](references/multi-tenancy.md) for schema-per-tenant and global filter patterns.

## Learn More

| Topic              | How to Find                                                                                  |
| ------------------ | -------------------------------------------------------------------------------------------- |
| EF Core overview   | `microsoft_docs_fetch(url="https://learn.microsoft.com/en-us/ef/core/")`                     |
| Compiled queries   | `microsoft_docs_search(query="EF Core compiled queries EF.CompileAsyncQuery")`               |
| Bulk ExecuteUpdate | `microsoft_docs_search(query="EF Core ExecuteUpdate ExecuteDelete bulk operations")`         |
| Interceptors       | `microsoft_docs_search(query="EF Core SaveChanges interceptors ISaveChangesInterceptor")`    |
| Owned entities     | `microsoft_docs_search(query="EF Core owned entity types value objects")`                    |
| TPH TPT TPC        | `microsoft_docs_search(query="EF Core table per hierarchy table per type inheritance")`      |
| Concurrency        | `microsoft_docs_search(query="EF Core optimistic concurrency rowversion concurrency token")` |
| Migrations prod    | `microsoft_docs_search(query="EF Core migrations production deployment idempotent script")`  |
