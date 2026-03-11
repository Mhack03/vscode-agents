# Worker Service — Standalone & Hosted

---

## Standalone Worker Service Project

```bash
# Create a dedicated worker project
dotnet new worker -n MyApp.Worker
```

```xml
<!-- MyApp.Worker.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Worker">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UserSecretsId>myapp-worker</UserSecretsId>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting"  Version="9.*" />
    <PackageReference Include="Serilog.Extensions.Hosting"   Version="*" />
  </ItemGroup>
</Project>
```

```csharp
// Program.cs — worker project
var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((ctx, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console())
    .ConfigureServices((ctx, services) =>
    {
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseSqlServer(ctx.Configuration.GetConnectionString("Sql")));
        services.AddScoped<IOrderProcessor, OrderProcessor>();
        services.AddHostedService<OrderProcessingWorker>();
        services.AddHostedService<CleanupWorker>();
    })
    .Build();

await host.RunAsync();
```

---

## BackgroundService Template

```csharp
public class OrderProcessingWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OrderProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OrderProcessingWorker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected — app is shutting down
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing order batch");
                // Wait before retry on failure
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                continue;
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        logger.LogInformation("OrderProcessingWorker stopped");
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        // Always create a new scope for scoped services (e.g., DbContext)
        await using var scope      = scopeFactory.CreateAsyncScope();
        var             processor  = scope.ServiceProvider
            .GetRequiredService<IOrderProcessor>();

        var processed = await processor.ProcessPendingBatchAsync(ct);
        if (processed > 0)
            logger.LogInformation("Processed {Count} orders", processed);
    }
}
```

---

## Graceful Shutdown

```csharp
public class GracefulWorker(
    IHostApplicationLifetime lifetime,
    ILogger<GracefulWorker> logger) : IHostedService
{
    public Task StartAsync(CancellationToken ct)
    {
        lifetime.ApplicationStarted.Register(() =>
            logger.LogInformation("Application started at {Time}", DateTime.UtcNow));

        lifetime.ApplicationStopping.Register(() =>
            logger.LogInformation("Application is stopping — draining queue..."));

        lifetime.ApplicationStopped.Register(() =>
            logger.LogInformation("Application stopped."));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

// appsettings.json — allow 30 seconds for graceful shutdown
{
  "ShutdownTimeout": "00:00:30"
}

// In Program.cs
builder.Services.Configure<HostOptions>(opts =>
    opts.ShutdownTimeout = TimeSpan.FromSeconds(30));
```

---

## One-Time Startup Task (Not Repeating)

```csharp
// Run DB migrations or seed data before app starts serving traffic
public class DatabaseMigratorService(IServiceScopeFactory scopeFactory,
    ILogger<DatabaseMigratorService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        logger.LogInformation("Running database migrations...");
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync(ct);
        logger.LogInformation("Database migrations complete.");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
```

---

## Cooperative Cancellation Patterns

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    // ✅ Always pass stoppingToken to all async calls
    await using var scope = scopeFactory.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // ✅ Create a linked token with a timeout
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
    cts.CancelAfter(TimeSpan.FromMinutes(5));   // Timeout individual work units

    try
    {
        await DoWorkAsync(cts.Token);
    }
    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
    {
        // Shutdown-initiated cancellation — not an error
    }
    catch (OperationCanceledException)
    {
        // Timeout — treat as error
        logger.LogWarning("Worker timed out during processing");
    }
}
```

---

## Learn More

| Topic | Query |
|-------|-------|
| Worker Service | `microsoft_docs_search(query="ASP.NET Core Worker Service BackgroundService IHostedService")` |
| Graceful shutdown | `microsoft_docs_search(query="ASP.NET Core graceful shutdown IHostApplicationLifetime stoppingToken")` |
| Scoped services in background | `microsoft_docs_search(query="IServiceScopeFactory DbContext BackgroundService scoped DI")` |
