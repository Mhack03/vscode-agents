---
name: dotnet-caching
description: Caching strategies for .NET 8/9/10 APIs — IMemoryCache, IDistributedCache with Redis, HybridCache (.NET 9), Output Cache, cache-aside pattern, ETag, and cache invalidation.
license: Complete terms in LICENSE.txt
---

# .NET Caching Patterns

## When to Use This Skill

- Adding in-process memory caching to a .NET service
- Setting up Redis distributed cache with StackExchange.Redis
- Configuring Output Caching middleware with tag-based invalidation
- Using HybridCache (.NET 9) for L1/L2 layered caching
- Implementing the cache-aside pattern in a repository or service
- Setting Cache-Control and ETag headers for HTTP-level caching
- Preventing cache stampede with locking

---

## Decision Guide

| Scenario                          | Use                              |
| --------------------------------- | -------------------------------- |
| Single instance, hot data         | `IMemoryCache`                   |
| Multiple instances / distributed  | `IDistributedCache` + Redis      |
| Both + automatic L1 fill from L2  | `HybridCache` (.NET 9+)          |
| Full HTTP response caching        | Output Cache middleware          |
| Browser / CDN caching             | `Cache-Control` + `ETag` headers |
| Very short-lived computed results | `Lazy<Task<T>>` + `MemoryCache`  |

---

## IMemoryCache

```csharp
builder.Services.AddMemoryCache();

// Usage in service
public class ProductService(IMemoryCache cache, IProductRepository repo)
{
    private static readonly MemoryCacheEntryOptions _opts = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        SlidingExpiration               = TimeSpan.FromMinutes(2),
        Size                            = 1,  // Requires cache.SizeLimit to be set
    };

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var key = $"product:{id}";
        if (cache.TryGetValue(key, out ProductDto? cached))
            return cached;

        var product = await repo.GetByIdAsync(id, ct);
        if (product is not null)
            cache.Set(key, product, _opts);

        return product;
    }

    public void Invalidate(int id) => cache.Remove($"product:{id}");
}
```

---

## Redis (IDistributedCache)

```bash
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
```

```csharp
// Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration         = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName          = "myapi:";  // Key prefix
    options.ConfigurationOptions  = new ConfigurationOptions
    {
        ConnectRetry = 5,
        ReconnectRetryPolicy = new LinearRetry(500),
    };
});

// Usage
public class CacheService(IDistributedCache cache)
{
    private static readonly JsonSerializerOptions _json = new()
        { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await cache.GetAsync(key, ct);
        return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes, _json);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null,
        CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _json);
        return cache.SetAsync(key, bytes,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? TimeSpan.FromMinutes(5),
            }, ct);
    }

    public Task RemoveAsync(string key, CancellationToken ct = default) =>
        cache.RemoveAsync(key, ct);
}
```

---

## HybridCache (.NET 9+) — Recommended

Combines L1 (in-process) + L2 (Redis) transparently:

```bash
dotnet add package Microsoft.Extensions.Caching.Hybrid
```

```csharp
// Program.cs
builder.Services.AddHybridCache(options =>
{
    options.MaximumPayloadBytes        = 1024 * 1024;  // 1 MB per entry
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration         = TimeSpan.FromMinutes(5),  // L2 (Redis) TTL
        LocalCacheExpiration = TimeSpan.FromMinutes(1), // L1 (memory) TTL
    };
});

// Usage — automatically handles stampede, serialization, L1/L2 fill
public class ProductService(HybridCache cache, IProductRepository repo)
{
    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await cache.GetOrCreateAsync(
            key:     $"product:{id}",
            factory: async token => await repo.GetByIdAsync(id, token),
            cancellationToken: ct);

    // Tag-based invalidation — removes all entries tagged "products"
    public async Task InvalidateProductAsync(int id, CancellationToken ct = default)
    {
        await cache.RemoveAsync($"product:{id}", ct);
        await cache.RemoveByTagAsync("products", ct);
    }
}
```

---

## Output Cache Middleware

```csharp
// Program.cs
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(b => b.Expire(TimeSpan.FromSeconds(30)));

    options.AddPolicy("Products", b =>
        b.Expire(TimeSpan.FromMinutes(5))
         .Tag("products")
         .SetVaryByQuery("page", "pageSize", "search", "sort"));

    options.AddPolicy("UserSpecific", b =>
        b.Expire(TimeSpan.FromMinutes(1))
         .SetVaryByHeader("Authorization")
         .NoStore());  // No cache for sensitive data
});

app.UseOutputCache();

// Apply on endpoints
products.MapGet("/", GetAll).CacheOutput("Products");
products.MapPost("/", Create).CacheOutput(b => b.NoStore());

// Invalidate on mutation
var cache = app.Services.GetRequiredService<IOutputCacheStore>();
await cache.EvictByTagAsync("products", ct);
```

---

## Cache-Aside Pattern

```csharp
// Generic cache-aside helper
public class CacheAside(HybridCache cache)
{
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        TimeSpan? ttl = null,
        string[]? tags = null,
        CancellationToken ct = default)
    {
        var entryOptions = ttl.HasValue
            ? new HybridCacheEntryOptions { Expiration = ttl.Value }
            : null;

        return await cache.GetOrCreateAsync(key, factory, entryOptions, tags, ct);
    }
}
```

---

## ETag / Cache-Control Headers

```csharp
// Middleware to add ETag and Cache-Control to GET responses
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Method == HttpMethods.Get)
        ctx.Response.GetTypedHeaders().CacheControl =
            new CacheControlHeaderValue
            {
                Public  = true,
                MaxAge  = TimeSpan.FromSeconds(60),
            };
    await next();
});

// ETag in endpoint
products.MapGet("/{id:int}", async (int id, IProductService svc, HttpContext ctx) =>
{
    var product = await svc.GetByIdAsync(id);
    if (product is null) return TypedResults.NotFound();

    var etag = $"\"{product.Version}\"";  // Version = row version hash

    if (ctx.Request.Headers.IfNoneMatch == etag)
        return TypedResults.StatusCode(304);  // Not Modified

    ctx.Response.Headers.ETag = etag;
    return TypedResults.Ok(product);
});
```

---

## References

| Topic                                                             | Load When                                            |
| ----------------------------------------------------------------- | ---------------------------------------------------- |
| [Redis Configuration](references/redis-setup.md)                  | Connection resilience, clustering, Sentinel, pub/sub |
| [Cache Invalidation Strategies](references/cache-invalidation.md) | Tag-based, event-driven, TTL strategy, write-through |

## Learn More

| Topic                   | Query                                                                                                  |
| ----------------------- | ------------------------------------------------------------------------------------------------------ |
| HybridCache             | `microsoft_docs_fetch(url="https://learn.microsoft.com/en-us/aspnet/core/performance/caching/hybrid")` |
| Output Cache            | `microsoft_docs_search(query="ASP.NET Core output caching middleware tag eviction .NET 9")`            |
| IDistributedCache Redis | `microsoft_docs_search(query="ASP.NET Core Redis distributed cache StackExchangeRedis")`               |
