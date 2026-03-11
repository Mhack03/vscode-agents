# EF Core Multi-Tenancy Patterns

---

## Strategy Comparison

| Strategy | Isolation | Complexity | Data Sharing | Best For |
|----------|-----------|------------|-------------|---------|
| Global Query Filter | Row-level | Low | Yes (same DB) | SaaS apps, simple isolation |
| Schema-per-Tenant | Schema-level | Medium | Possible | Moderate isolation needs |
| Database-per-Tenant | Database-level | High | None | Strict compliance, large tenants |

---

## Approach 1: Row-level Isolation with Global Query Filter

```csharp
// ICurrentTenantService — abstraction for the current tenant
public interface ICurrentTenantService
{
    Guid? TenantId { get; }
}

// Implementation — reads from JWT claim or HTTP header
public class HttpTenantService(IHttpContextAccessor httpContext) : ICurrentTenantService
{
    public Guid? TenantId =>
        Guid.TryParse(
            httpContext.HttpContext?.User.FindFirstValue("tenant_id")
            ?? httpContext.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault(),
            out var id) ? id : null;
}

// DbContext — apply global query filter
public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentTenantService tenantService) : DbContext(options)
{
    private Guid? TenantId => tenantService.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply tenant filter to all tenant-owned entities
        modelBuilder.Entity<Product>()
            .HasQueryFilter(p => TenantId == null || p.TenantId == TenantId);

        modelBuilder.Entity<Order>()
            .HasQueryFilter(o => TenantId == null || o.TenantId == TenantId);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Automatically set TenantId on new entities
        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added &&
                        e.Entity is ITenantEntity))
        {
            if (TenantId.HasValue)
                ((ITenantEntity)entry.Entity).TenantId = TenantId.Value;
        }

        return await base.SaveChangesAsync(ct);
    }
}

// Base interface for tenant-owned entities
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}

// Entity with tenant support
public class Product : ITenantEntity
{
    public int  Id       { get; set; }
    public Guid TenantId { get; set; }
    public string Name   { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

```csharp
// Bypassing filter for admin/system queries
var allProducts = await context.Products
    .IgnoreQueryFilters()
    .ToListAsync(ct);
```

---

## Approach 2: Schema-per-Tenant

```csharp
// Separate migration per schema
public class TenantDbContext(
    DbContextOptions<TenantDbContext> options,
    string schema) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(schema);
        // All tables go into tenant's schema: tenant1.Products, tenant2.Products
    }
}

// Factory — creates context for specific tenant
public class TenantDbContextFactory(IConfiguration config)
{
    public TenantDbContext Create(string tenantId)
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(config.GetConnectionString("Default"))
            .Options;

        return new TenantDbContext(options, $"tenant_{tenantId}");
    }
}

// Migration per schema
// dotnet ef migrations add InitTenant --schema tenant_acme
```

---

## Approach 3: Database-per-Tenant

```csharp
// TenantRegistry — stores connection strings per tenant
public class TenantRegistry(IDistributedCache cache, ITenantRepository repo)
{
    public async Task<string> GetConnectionStringAsync(Guid tenantId)
    {
        var cacheKey = $"tenant-conn:{tenantId}";
        var cached   = await cache.GetStringAsync(cacheKey);

        if (cached is not null) return cached;

        var tenant = await repo.FindByIdAsync(tenantId)
            ?? throw new TenantNotFoundException(tenantId);

        await cache.SetStringAsync(cacheKey, tenant.ConnectionString,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

        return tenant.ConnectionString;
    }
}

// Dynamic DbContext per request
public class TenantDbContextFactory(
    ICurrentTenantService tenantService,
    TenantRegistry registry)
{
    public async Task<AppDbContext> CreateAsync()
    {
        var tenantId = tenantService.TenantId
            ?? throw new UnauthorizedAccessException("No tenant context.");

        var connStr  = await registry.GetConnectionStringAsync(tenantId);

        var options  = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connStr)
            .Options;

        return new AppDbContext(options);
    }
}
```

---

## Migrations for Multiple Tenants

```csharp
// Apply migrations to all tenants on startup
public class MultiTenantMigrator(
    ITenantRepository tenantRepo,
    IServiceProvider sp,
    ILogger<MultiTenantMigrator> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var tenants = await tenantRepo.GetAllAsync(ct);

        await Parallel.ForEachAsync(tenants,
            new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct },
            async (tenant, innerCt) =>
            {
                try
                {
                    using var scope = sp.CreateScope();
                    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await ctx.Database.MigrateAsync(innerCt);
                    logger.LogInformation("Migrated tenant {TenantId}", tenant.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to migrate tenant {TenantId}", tenant.Id);
                }
            });
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
```

---

## Learn More

| Topic | Query |
|-------|-------|
| Multi-tenancy EF Core | `microsoft_docs_search(query="EF Core multi-tenancy global query filter tenant")` |
| Schema per tenant | `microsoft_docs_search(query="EF Core schema per tenant HasDefaultSchema migrations")` |
| Connection per tenant | `microsoft_docs_search(query="EF Core dynamic DbContext connection string per tenant factory")` |
