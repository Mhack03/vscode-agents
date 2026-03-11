// cache-service.cs — Generic cache-aside service using HybridCache
// Demonstrates: GetOrCreateAsync, tag invalidation, Redis fallback

using Microsoft.Extensions.Caching.Hybrid;

// ─── Generic HybridCache Service ─────────────────────────────────────────────
public class CacheService(HybridCache cache)
{
    // Get or create with automatic serialization
    public Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        TimeSpan? expiration = null,
        IEnumerable<string>? tags = null,
        CancellationToken ct = default) where T : notnull
    {
        var options = expiration is not null
            ? new HybridCacheEntryOptions { Expiration = expiration }
            : null;

        return cache.GetOrCreateAsync(key, factory, options,
            tags: tags?.ToArray(), cancellationToken: ct).AsTask();
    }

    public Task RemoveAsync(string key, CancellationToken ct = default) =>
        cache.RemoveAsync(key, ct).AsTask();

    public Task RemoveByTagAsync(string tag, CancellationToken ct = default) =>
        cache.RemoveByTagAsync(tag, ct).AsTask();
}

// ─── Product Service wrapping cache ──────────────────────────────────────────
public class ProductCacheService(
    AppDbContext db,
    CacheService cache,
    ILogger<ProductCacheService> logger)
{
    // Cache key helpers
    private static string ItemKey(int id) => $"product:{id}";
    private const string ListTag = "product-list";

    // ─── Read-through ─────────────────────────────────────────────────────────
    public Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        return cache.GetOrCreateAsync<ProductDto?>(
            ItemKey(id),
            async token =>
            {
                var p = await db.Products
                    .Include(x => x.Category)
                    .FirstOrDefaultAsync(x => x.Id == id, token);
                return p?.ToDto();
            },
            expiration: TimeSpan.FromMinutes(30),
            tags: [ItemKey(id), ListTag],
            ct: ct);
    }

    public Task<PagedResponse<ProductDto>> GetPageAsync(
        ProductFilter filter, CancellationToken ct)
    {
        // Cache key includes all filter params for unique pages
        var key = $"products:{filter.Page}:{filter.PageSize}:" +
                  $"{filter.Category}:{filter.Sort}:{filter.MinPrice}:{filter.MaxPrice}";

        return cache.GetOrCreateAsync<PagedResponse<ProductDto>>(
            key,
            async token =>
            {
                var query = db.Products
                    .Include(p => p.Category)
                    .ApplyFilter(filter);
                var page = await query.ToPagedAsync(filter.Page, filter.PageSize, token);
                return new PagedResponse<ProductDto>(
                    page.Items.Select(p => p.ToDto()).ToList(),
                    page.Page, page.PageSize, page.TotalCount);
            },
            expiration: TimeSpan.FromMinutes(5),
            tags: [ListTag],
            ct: ct);
    }

    // ─── Write-through (update + invalidate) ─────────────────────────────────
    public async Task<ProductDto> CreateAsync(CreateProductRequest req, CancellationToken ct)
    {
        var product = new Product
        {
            Name = req.Name,
            Sku = req.Sku,
            Price = req.Price,
            CategoryId = req.CategoryId,
        };
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        // Invalidate all product lists
        await cache.RemoveByTagAsync(ListTag, ct);

        logger.LogInformation("Created product {ProductId}, invalidated list cache", product.Id);
        return product.ToDto();
    }

    public async Task<ProductDto?> UpdateAsync(
        int id, UpdateProductRequest req, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct);
        if (product is null) return null;

        if (req.Name is not null) product.Name = req.Name;
        if (req.Price is not null) product.Price = req.Price.Value;
        await db.SaveChangesAsync(ct);

        // Invalidate specific entry + all lists
        await cache.RemoveAsync(ItemKey(id), ct);
        await cache.RemoveByTagAsync(ListTag, ct);

        return product.ToDto();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var deleted = await db.Products
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync(ct);

        if (deleted > 0)
        {
            await cache.RemoveAsync(ItemKey(id), ct);
            await cache.RemoveByTagAsync(ListTag, ct);
        }

        return deleted > 0;
    }
}
