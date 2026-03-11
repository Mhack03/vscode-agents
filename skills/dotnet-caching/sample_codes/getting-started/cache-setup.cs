// cache-setup.cs — HybridCache + Redis + Output Cache registration
// Demonstrates: Program.cs wiring for all caching strategies

var builder = WebApplication.CreateBuilder(args);

// ─── IMemoryCache (in-process, single instance) ───────────────────────────────
builder.Services.AddMemoryCache(opts =>
{
    opts.SizeLimit = 1024;   // Entries limit
    opts.CompactionPercentage = 0.25;   // Remove 25% when limit hit
});

// ─── Redis (distributed, multi-instance) ─────────────────────────────────────
builder.Services.AddStackExchangeRedisCache(opts =>
{
    opts.Configuration = builder.Configuration.GetConnectionString("Redis");
    opts.InstanceName = "myapi:";
});

// Register IConnectionMultiplexer for pub/sub and direct commands
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(new StackExchange.Redis.ConfigurationOptions
    {
        EndPoints = { builder.Configuration.GetConnectionString("Redis")! },
        AbortOnConnectFail = false,
        ConnectRetry = 3,
        ReconnectRetryPolicy = new StackExchange.Redis.ExponentialRetry(5_000),
    }));

// ─── HybridCache (.NET 9+ — local L1 + distributed L2) ───────────────────────
builder.Services.AddHybridCache(opts =>
{
    opts.MaximumPayloadBytes = 1_024 * 1_024;   // 1 MB per entry
    opts.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2),
    };
});

// ─── Output Cache (HTTP-level) ────────────────────────────────────────────────
builder.Services.AddOutputCache(opts =>
{
    // Default: short TTL, vary by Accept header
    opts.AddBasePolicy(b => b
        .Expire(TimeSpan.FromSeconds(30))
        .With(r => r.HttpContext.Request.Method == HttpMethods.Get)
        .Tag("global"));

    // Products page: 2 minute cache, vary by query string
    opts.AddPolicy("Products", b => b
        .Expire(TimeSpan.FromMinutes(2))
        .SetVaryByQuery("*")
        .Tag("product-list"));

    // Individual product: 10 minutes
    opts.AddPolicy("Product", b => b
        .Expire(TimeSpan.FromMinutes(10))
        .SetVaryByRouteValue("id")
        .Tag("product-list"));
});

var app = builder.Build();

app.UseOutputCache();

// ─── Cache-enabled endpoints ──────────────────────────────────────────────────
var products = app.MapGroup("/api/products");

// List — cached 2 minutes, vary by QS
products.MapGet("", GetProducts).CacheOutput("Products");

// Single product — cached 10 minutes
products.MapGet("{id:int}", GetProduct).CacheOutput("Product");

// Mutation — evict caches
products.MapPost("", async (
    CreateProductRequest req,
    IProductService svc,
    IOutputCacheStore cacheStore,
    CancellationToken ct) =>
{
    var product = await svc.CreateAsync(req, ct);
    await cacheStore.EvictByTagAsync("product-list", ct);
    return TypedResults.Created($"/api/products/{product.Id}", product);
});

// Placeholder handlers
static Task<IResult> GetProducts(IProductService svc, CancellationToken ct)
    => Task.FromResult<IResult>(TypedResults.Ok<object>(null!));

static Task<IResult> GetProduct(int id, IProductService svc, CancellationToken ct)
    => Task.FromResult<IResult>(TypedResults.Ok<object>(null!));

app.Run();
