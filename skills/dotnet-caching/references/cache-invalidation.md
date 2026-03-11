# Cache Invalidation Strategies

---

## Strategy Overview

| Strategy | When to Use | Complexity |
|----------|-------------|------------|
| TTL (expiry only) | Data can be slightly stale; simple queries | Low |
| Explicit key delete | CRUD endpoints that update specific records | Low |
| Tag-based invalidation | Many keys share a logical group | Medium |
| Event-driven (pub/sub) | Multi-instance deployments | Medium |
| Write-through | Reads are very frequent; writes less so | High |
| Read-through with refresh | Background refresh before expiry | High |

---

## Explicit Key Invalidation Pattern

```csharp
public class ProductService(
    AppDbContext db,
    IHybridCacheService cache)
{
    private static string ProductKey(int id)       => $"product:{id}";
    private static string ProductListKey(string? cat) => $"products:list:{cat ?? "all"}";

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await cache.GetOrCreateAsync(
            ProductKey(id),
            async ct2 => (await db.Products.FindAsync([id], ct2))?.ToDto(),
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(30) },
            cancellationToken: ct);
    }

    public async Task UpdateAsync(int id, UpdateProductRequest req, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct)
            ?? throw new NotFoundException($"Product {id} not found");

        product.Name  = req.Name ?? product.Name;
        product.Price = req.Price ?? product.Price;
        await db.SaveChangesAsync(ct);

        // Invalidate specific product and any list caches
        await cache.RemoveAsync(ProductKey(id), ct);
        await cache.RemoveByTagAsync("product-list", ct);  // Remove all list pages
    }
}
```

---

## Tag-Based Invalidation (HybridCache)

```csharp
// HybridCache supports tags per entry
await cache.GetOrCreateAsync(
    $"products:page:{page}",
    async ct => await LoadPage(page, ct),
    new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) },
    tags: ["product-list", $"category:{categoryId}"],
    cancellationToken: ct);

// Invalidate all pages when any product changes
await cache.RemoveByTagAsync("product-list", ct);

// Invalidate only pages for a specific category
await cache.RemoveByTagAsync($"category:{categoryId}", ct);
```

---

## Event-Driven Invalidation (Redis Pub/Sub)

Useful when multiple API instances each have in-memory caches:

```csharp
// Publisher (when data changes)
public class CacheEventPublisher(IConnectionMultiplexer redis)
{
    public Task InvalidateAsync(string cacheKey) =>
        redis.GetSubscriber().PublishAsync(
            RedisChannel.Literal("cache:invalidate"),
            cacheKey);
}

// Subscriber (runs at startup in each instance)
public class CacheInvalidationListener(
    IConnectionMultiplexer redis,
    IMemoryCache           memoryCache,
    ILogger<CacheInvalidationListener> logger) : IHostedService
{
    public Task StartAsync(CancellationToken ct)
    {
        redis.GetSubscriber().Subscribe(
            RedisChannel.Literal("cache:invalidate"),
            (channel, key) =>
            {
                logger.LogInformation("Cache invalidation received: {Key}", key);
                memoryCache.Remove(key.ToString());
            });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        redis.GetSubscriber().UnsubscribeAll();
        return Task.CompletedTask;
    }
}
```

---

## Stampede Prevention (SemaphoreSlim)

When cache expires, prevents 100 simultaneous DB queries racing:

```csharp
public class StampedeProtectedCache<T>(
    IDistributedCache cache,
    ILogger<StampedeProtectedCache<T>> logger)
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<T?> GetOrCreateAsync(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        // Fast path — value in cache
        var cached = await cache.GetAsync(key, ct);
        if (cached is not null)
            return JsonSerializer.Deserialize<T>(cached);

        // Slow path — acquire per-key lock
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            cached = await cache.GetAsync(key, ct);
            if (cached is not null)
                return JsonSerializer.Deserialize<T>(cached);

            logger.LogDebug("Cache miss for {Key}, loading from source", key);
            var value = await factory(ct);
            if (value is not null)
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
                await cache.SetAsync(key, bytes,
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry },
                    ct);
            }
            return value;
        }
        finally
        {
            semaphore.Release();
            _locks.TryRemove(key, out _);
        }
    }
}
```

---

## Learn More

| Topic | Query |
|-------|-------|
| HybridCache tags | `microsoft_docs_search(query="HybridCache RemoveByTagAsync tag invalidation .NET 9")` |
| Cache stampede | `microsoft_docs_search(query="cache stampede prevention thundering herd .NET SemaphoreSlim")` |
| Redis pub/sub | `microsoft_docs_search(query="Redis publish subscribe cache invalidation StackExchange.Redis")` |
