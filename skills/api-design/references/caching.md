# Caching Strategies

## Redis Cache Middleware

### Basic Cache Middleware

```javascript
const redis = require("redis").createClient();

function cacheMiddleware(duration = 300) {
	return (req, res, next) => {
		// Skip caching for non-GET or authenticated requests with user-specific data
		if (req.method !== "GET" || req.query.skipCache) {
			return next();
		}

		const cacheKey = `cache:${req.originalUrl}`;

		// Try to get from cache
		redis.get(cacheKey, (err, cached) => {
			if (err) {
				console.error("Cache error:", err);
				return next();
			}

			if (cached) {
				res.set("X-Cache", "HIT");
				return res.json(JSON.parse(cached));
			}

			res.set("X-Cache", "MISS");

			// Override res.json to cache response
			const originalJson = res.json.bind(res);
			res.json = (data) => {
				// Only cache successful responses
				if (res.statusCode === 200) {
					redis.setex(cacheKey, duration, JSON.stringify(data), (err) => {
						if (err) console.error("Cache set error:", err);
					});
				}
				return originalJson(data);
			};

			next();
		});
	};
}

// Usage
app.get("/api/products", cacheMiddleware(600), getProductsController);
app.get("/api/categories", cacheMiddleware(3600), getCategoriesController);
```

### Cache Invalidation

```javascript
function cacheInvalidate(pattern) {
	return async (req, res, next) => {
		// After successful mutation, invalidate related cache
		const originalJson = res.json.bind(res);
		res.json = (data) => {
			// Invalidate cache matching pattern
			redis.keys(`cache:${pattern}*`, (err, keys) => {
				if (err) return;
				if (keys.length > 0) {
					redis.del(...keys, (err) => {
						if (err) console.error("Cache invalidation error:", err);
					});
				}
			});

			return originalJson(data);
		};

		next();
	};
}

// Usage
app.post(
	"/api/products",
	authenticate,
	authorize("admin"),
	cacheInvalidate("/api/products*"),
	createProductController
);
```

## User-Specific Caching

```javascript
function userCacheMiddleware(duration = 300) {
	return (req, res, next) => {
		if (req.method !== "GET") return next();

		// Include user ID in cache key for user-specific data
		const cacheKey = req.user
			? `cache:user-${req.user.id}:${req.originalUrl}`
			: `cache:${req.originalUrl}`;

		redis.get(cacheKey, (err, cached) => {
			if (cached) {
				res.set("X-Cache", "HIT");
				return res.json(JSON.parse(cached));
			}

			const originalJson = res.json.bind(res);
			res.json = (data) => {
				redis.setex(cacheKey, duration, JSON.stringify(data), (err) => {
					if (err) console.error("Cache error:", err);
				});
				return originalJson(data);
			};

			next();
		});
	};
}

app.use("/api/", authenticate, userCacheMiddleware(300));
```

## Cache Warming

```javascript
async function warmCache() {
	const cachePatterns = [
		{ key: "/api/categories", duration: 3600, fetcher: getCategories },
		{
			key: "/api/popular-products",
			duration: 300,
			fetcher: getPopularProducts,
		},
	];

	for (const pattern of cachePatterns) {
		try {
			const data = await pattern.fetcher();
			await redis.setex(
				`cache:${pattern.key}`,
				pattern.duration,
				JSON.stringify(data)
			);
			console.log(`Cache warmed: ${pattern.key}`);
		} catch (error) {
			console.error(`Failed to warm cache for ${pattern.key}:`, error);
		}
	}
}

// Run on startup
app.listen(3000, () => {
	console.log("Server started");
	warmCache();
});

// Optionally run periodically
setInterval(warmCache, 60 * 1000); // Every minute
```

## ASP.NET Core Response Caching

### Output Cache (Built-in)

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    [OutputCache(PolicyName = "CacheProducts")]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _repository.GetProductsAsync();
        return Ok(products);
    }
}

// Configure in Startup
public void ConfigureServices(IServiceCollection services)
{
    services.AddOutputCache(options =>
    {
        options.AddPolicy("CacheProducts", builder =>
            builder.Expire(TimeSpan.FromMinutes(10))
                   .WithTag("products"));

        options.AddPolicy("CacheUserData", builder =>
            builder.Expire(TimeSpan.FromMinutes(5))
                   .VaryByHeader("Authorization"));
    });
}

public void Configure(IApplicationBuilder app)
{
    app.UseOutputCache();
}
```

### Cache Invalidation

```csharp
[HttpPost]
public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
{
    var product = await _repository.CreateProductAsync(dto);

    // Invalidate cache
    await _cache.RemoveAsync("cache:/api/products");

    return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
}
```

### Distributed Cache (Redis)

```csharp
public class ProductService
{
    private readonly IDistributedCache _cache;
    private readonly IRepository _repository;

    public async Task<List<Product>> GetProductsAsync()
    {
        const string cacheKey = "products:all";

        // Try to get from cache
        var cached = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<List<Product>>(cached);
        }

        // Get from database
        var products = await _repository.GetProductsAsync();

        // Store in cache for 10 minutes
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(products),
            options
        );

        return products;
    }
}

// Register in Startup
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = Configuration.GetConnectionString("Redis");
});
```

## HybridCache (ASP.NET Core 9+)

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly HybridCache _cache;
    private readonly IRepository _repository;

    public ProductsController(HybridCache cache, IRepository repository)
    {
        _cache = cache;
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _cache.GetOrCreateAsync(
            "products:all",
            async cancel => await _repository.GetProductsAsync(cancel),
            options: new HybridCacheEntryOptions
            {
                LocalCacheDuration = TimeSpan.FromMinutes(1),
                DistributedCacheDuration = TimeSpan.FromMinutes(10)
            }
        );

        return Ok(products);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
    {
        var product = await _repository.CreateProductAsync(dto);

        // Remove from cache
        await _cache.RemoveAsync("products:all");

        return CreatedAtAction(nameof(GetProduct), product);
    }
}

// Configure in Startup
services.AddHybridCache();
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = Configuration.GetConnectionString("Redis");
});
```

## Cache Strategies

### Cache-Aside Pattern

```javascript
async function getUser(userId) {
	const cacheKey = `user:${userId}`;

	// 1. Try cache
	const cached = await redis.get(cacheKey);
	if (cached) return JSON.parse(cached);

	// 2. Get from database
	const user = await db.users.findById(userId);

	// 3. Store in cache
	await redis.setex(cacheKey, 3600, JSON.stringify(user));

	return user;
}
```

### Write-Through Pattern

```javascript
async function updateUser(userId, updates) {
	const cacheKey = `user:${userId}`;

	// 1. Update database
	const user = await db.users.update(userId, updates);

	// 2. Update cache
	await redis.setex(cacheKey, 3600, JSON.stringify(user));

	return user;
}
```

### Write-Behind Pattern

```javascript
const updateQueue = [];

function updateUserAsync(userId, updates) {
	// Update cache immediately
	redis.setex(`user:${userId}`, 3600, JSON.stringify(updates));

	// Queue database update
	updateQueue.push({ userId, updates, timestamp: Date.now() });
}

// Process queue periodically
setInterval(async () => {
	while (updateQueue.length > 0) {
		const { userId, updates } = updateQueue.shift();
		try {
			await db.users.update(userId, updates);
		} catch (error) {
			// Re-queue on failure
			updateQueue.unshift({ userId, updates });
			break;
		}
	}
}, 5000);
```

## Best Practices

1. **Set appropriate TTL** - Balance freshness vs. performance
2. **Use cache warming** - Pre-load hot data
3. **Implement invalidation** - Clear cache on mutations
4. **Monitor cache hit rate** - Aim for >80%
5. **Handle cache misses** - Don't let them cascade
6. **Use cache tags** - Group related cache entries
7. **Encrypt sensitive data** - In cache layer
8. **Document cache keys** - For debugging
9. **Implement cache versioning** - For schema changes
10. **Test cache behavior** - Include in integration tests
