# Redis Configuration & Resilience

---

## StackExchange.Redis Setup

```bash
dotnet add package StackExchange.Redis
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
```

```csharp
// Program.cs — register both IConnectionMultiplexer and IDistributedCache
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(new ConfigurationOptions
    {
        EndPoints          = { builder.Configuration.GetConnectionString("Redis")! },
        ConnectRetry       = 5,
        ConnectTimeout     = 5_000,
        SyncTimeout        = 5_000,
        AbortOnConnectFail = false,       // App starts even if Redis is down
        ReconnectRetryPolicy = new ExponentialRetry(5_000),
        DefaultDatabase    = 0,
        KeepAlive          = 60,
        Password           = builder.Configuration["Redis:Password"],
        Ssl                = !builder.Environment.IsDevelopment(),
    }));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName  = "myapi:";  // Prefix all keys
});
```

---

## Redis Pub/Sub (Event Broadcasting)

Useful for broadcasting cache invalidations or events across API instances:

```csharp
public class RedisEventBus(IConnectionMultiplexer redis)
{
    private readonly ISubscriber _sub = redis.GetSubscriber();

    public async Task PublishAsync<T>(string channel, T message)
    {
        var json = JsonSerializer.Serialize(message);
        await _sub.PublishAsync(RedisChannel.Literal(channel), json);
    }

    public void Subscribe<T>(string channel, Action<T> handler)
    {
        _sub.Subscribe(RedisChannel.Literal(channel), (_, value) =>
        {
            var msg = JsonSerializer.Deserialize<T>(value.ToString()!);
            if (msg is not null) handler(msg);
        });
    }
}

// Usage — broadcast cache invalidation to all instances
await eventBus.PublishAsync("cache:invalidate", new { Key = $"product:{id}" });
```

---

## Direct Redis Commands (IDatabase)

When `IDistributedCache` is not enough:

```csharp
public class RedisService(IConnectionMultiplexer redis)
{
    private readonly IDatabase _db = redis.GetDatabase();

    // Atomic increment (e.g., rate limiting counter)
    public async Task<long> IncrementAsync(string key, TimeSpan expiry)
    {
        var count = await _db.StringIncrementAsync(key);
        if (count == 1)
            await _db.KeyExpireAsync(key, expiry);  // Set TTL on first increment
        return count;
    }

    // Distributed lock (prevent concurrent execution)
    public async Task<bool> AcquireLockAsync(string resource, TimeSpan timeout)
    {
        var lockKey = $"lock:{resource}";
        return await _db.StringSetAsync(
            lockKey, Environment.MachineName,
            timeout, When.NotExists);
    }

    public Task ReleaseLockAsync(string resource) =>
        _db.KeyDeleteAsync($"lock:{resource}");

    // Sorted set — leaderboard / rate window
    public Task AddToSortedSetAsync(string key, string member, double score) =>
        _db.SortedSetAddAsync(key, member, score);

    public async Task<SortedSetEntry[]> GetTopNAsync(string key, int n) =>
        await _db.SortedSetRangeByRankWithScoresAsync(key, 0, n - 1,
            Order.Descending);
}
```

---

## Redis Cluster / Sentinel (Production)

```csharp
// Sentinel (automatic failover)
var options = new ConfigurationOptions
{
    ServiceName = "mymaster",  // Sentinel master name
    EndPoints = {
        { "sentinel1", 26379 },
        { "sentinel2", 26379 },
        { "sentinel3", 26379 },
    },
    AbortOnConnectFail = false,
    Password           = "yourpassword",
};

// Cluster (sharding)
var clusterOptions = new ConfigurationOptions
{
    EndPoints     = { "redis-node1:6379", "redis-node2:6379", "redis-node3:6379" },
    CommandMap    = CommandMap.Create(new HashSet<string> { "CLUSTER" }),
};
```

---

## Circuit Breaker for Redis (Polly)

```bash
dotnet add package Polly.Extensions.Http
dotnet add package Microsoft.Extensions.Http.Resilience
```

```csharp
// Gracefully degrade when Redis is unavailable
public class ResilientCacheService(
    IDistributedCache cache,
    ILogger<ResilientCacheService> logger)
{
    private readonly ResiliencePipeline _pipeline = new ResiliencePipelineBuilder()
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio             = 0.5,
            SamplingDuration         = TimeSpan.FromSeconds(30),
            MinimumThroughput        = 10,
            BreakDuration            = TimeSpan.FromSeconds(30),
            OnOpened = args =>
            {
                logger.LogWarning("Redis circuit breaker opened: {Reason}", args.Outcome.Exception?.Message);
                return ValueTask.CompletedTask;
            }
        })
        .Build();

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            return await _pipeline.ExecuteAsync(async token =>
            {
                var bytes = await cache.GetAsync(key, token);
                return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes);
            }, ct);
        }
        catch (BrokenCircuitException)
        {
            logger.LogWarning("Redis unavailable, proceeding without cache for key {Key}", key);
            return default;
        }
    }
}
```

---

## Learn More

| Topic | Query |
|-------|-------|
| StackExchange.Redis | `microsoft_docs_search(query="StackExchange.Redis configuration cluster sentinel .NET")` |
| Distributed lock | `microsoft_docs_search(query="Redis distributed lock Redlock .NET StackExchange.Redis")` |
| Polly resilience | `microsoft_docs_search(query="Polly circuit breaker resilience pipeline .NET 9")` |
