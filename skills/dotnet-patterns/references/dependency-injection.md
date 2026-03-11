# Dependency Injection Patterns in .NET

Reference for advanced DI patterns using Microsoft.Extensions.DependencyInjection.

---

## Service Lifetimes

| Lifetime | Created | Disposed | Use For |
|----------|---------|----------|---------|
| `Singleton` | Once, app start | App shutdown | Caches, config, client pools |
| `Scoped` | Once per HTTP request | End of request | DbContext, unit-of-work, per-request state |
| `Transient` | Every time injected | End of enclosing scope | Lightweight stateless services |

### Captive Dependency Rule

> **Never inject a shorter-lived service into a longer-lived one.**

```
Singleton  → can receive: Singleton
Scoped     → can receive: Singleton, Scoped
Transient  → can receive: Singleton, Scoped, Transient
```

Injecting a `Scoped` into a `Singleton` causes the scoped service to live for the app lifetime — a captive dependency bug. ASP.NET Core validates this in `Development`:

```csharp
builder.Host.UseDefaultServiceProvider(o =>
    o.ValidateScopes = true);  // default true in Development
```

### Using Scoped Services from Singleton

When a singleton genuinely needs scoped work (e.g., a background job reading from `DbContext`):

```csharp
public class BackgroundWorker(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            await repo.ProcessPendingAsync(ct);
            await Task.Delay(TimeSpan.FromMinutes(1), ct);
        }
    }
}
```

---

## Registration Patterns

### Basic Registration
```csharp
services.AddScoped<IOrderRepository, OrderRepository>();
services.AddScoped<IOrderService, OrderService>();
services.AddSingleton<ICacheService, MemoryCacheService>();
services.AddTransient<IEmailSender, SmtpEmailSender>();
```

### Factory Registration

Use when construction requires runtime logic:

```csharp
services.AddScoped<IPaymentProcessor>(sp =>
{
    var config = sp.GetRequiredService<IOptions<PaymentSettings>>().Value;
    return config.Provider switch
    {
        "Stripe"  => new StripeProcessor(config.ApiKey),
        "Braintree" => new BraintreeProcessor(config.ApiKey),
        _ => throw new InvalidOperationException($"Unknown provider: {config.Provider}")
    };
});
```

### Keyed Services (.NET 8+)

Register multiple implementations of the same interface, differentiated by a key:

```csharp
// Registration
services.AddKeyedScoped<INotificationSender, EmailSender>("email");
services.AddKeyedScoped<INotificationSender, SlackSender>("slack");

// Consumption — via attribute in constructor
public class AlertService(
    [FromKeyedServices("email")] INotificationSender emailSender,
    [FromKeyedServices("slack")] INotificationSender slackSender) { }

// Or via IServiceProvider
var emailSender = provider.GetRequiredKeyedService<INotificationSender>("email");
```

### Register All Implementations

```csharp
// Register all IValidator<T> implementations in the assembly
var assembly = typeof(Program).Assembly;
foreach (var type in assembly.GetTypes()
    .Where(t => t.IsClass && !t.IsAbstract)
    .Where(t => t.GetInterfaces().Any(i =>
        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))))
{
    var iface = type.GetInterfaces().First(i =>
        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));
    services.AddScoped(iface, type);
}
```

### Extension Method Convention

Group related registrations as extension methods to keep `Program.cs` clean:

```csharp
// Infrastructure/DependencyInjection.cs
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseSqlServer(configuration.GetConnectionString("Default")));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        return services;
    }
}

// Program.cs — clean single line
builder.Services.AddInfrastructure(builder.Configuration);
```

---

## Options Pattern

### Basic Usage

```csharp
// Settings POCO
public class EmailSettings
{
    public const string SectionName = "Email";

    public required string Host    { get; init; }
    public required int    Port    { get; init; }
    public required string Sender  { get; init; }
    public bool UseSsl { get; init; } = true;
}

// appsettings.json
// {
//   "Email": {
//     "Host": "smtp.example.com",
//     "Port": 587,
//     "Sender": "no-reply@example.com"
//   }
// }

// Registration
services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

// Or with validation
services.AddOptions<EmailSettings>()
    .BindConfiguration(EmailSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();  // Fail fast at startup if invalid
```

### IOptions vs IOptionsSnapshot vs IOptionsMonitor

| Interface | Lifetime | Hot Reload | Use For |
|-----------|----------|------------|---------|
| `IOptions<T>` | Singleton | ❌ | Static config, singletons |
| `IOptionsSnapshot<T>` | Scoped | ✅ | Per-request config, scoped services |
| `IOptionsMonitor<T>` | Singleton | ✅ | Singletons that need live config changes |

```csharp
// IOptionsMonitor — live changes without restart
public class FeatureFlagService(IOptionsMonitor<FeatureFlags> monitor)
{
    public bool IsDarkModeEnabled => monitor.CurrentValue.DarkMode;

    public FeatureFlagService(IOptionsMonitor<FeatureFlags> monitor)
    {
        // Subscribe to changes
        monitor.OnChange(flags =>
            Console.WriteLine($"Flags changed: DarkMode={flags.DarkMode}"));
    }
}
```

### Validation with Data Annotations

```csharp
public class JwtSettings
{
    [Required, MinLength(32)]
    public required string Secret   { get; init; }

    [Required]
    public required string Issuer   { get; init; }

    [Required]
    public required string Audience { get; init; }

    [Range(1, 1440)]
    public int ExpiryMinutes { get; init; } = 60;
}

services.AddOptions<JwtSettings>()
    .BindConfiguration("JwtSettings")
    .ValidateDataAnnotations()
    .ValidateOnStart();  // Throws at startup if invalid
```

---

## Decorator Pattern

Wrap an existing service with cross-cutting behavior (logging, caching, retry):

```csharp
// Cache decorator for IOrderRepository
public class CachedOrderRepository(
    IOrderRepository inner,
    IMemoryCache cache) : IOrderRepository
{
    private static string Key(Guid id) => $"order:{id}";

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (cache.TryGetValue(Key(id), out Order? cached)) return cached;

        var order = await inner.GetByIdAsync(id, ct);
        if (order is not null)
            cache.Set(Key(id), order, TimeSpan.FromMinutes(5));

        return order;
    }

    // Delegate everything else
    public Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct) =>
        inner.GetAllAsync(ct);

    public void Add(Order order) => inner.Add(order);

    public Task<int> SaveChangesAsync(CancellationToken ct) =>
        inner.SaveChangesAsync(ct);
}

// Registration — Scrutor library makes this cleaner
services.AddScoped<IOrderRepository, OrderRepository>();
services.Decorate<IOrderRepository, CachedOrderRepository>(); // using Scrutor
```

---

## Generic Repository Registration

```csharp
// Generic interface
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    void Add(T entity);
    void Remove(T entity);
}

// Generic implementation
public class EfRepository<T>(AppDbContext db) : IRepository<T> where T : class
{
    private DbSet<T> Set => db.Set<T>();

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Set.FindAsync([id], ct).AsTask();

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct) =>
        await Set.AsNoTracking().ToListAsync(ct);

    public void Add(T entity)    => Set.Add(entity);
    public void Remove(T entity) => Set.Remove(entity);
}

// Registration
services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
```

---

## Learn More

| Topic | Search |
|-------|--------|
| DI docs | `microsoft_docs_fetch(url="https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection")` |
| Keyed services | `microsoft_docs_search(query="aspnet core keyed services fromkeyedservices")` |
| Options validation | `microsoft_docs_search(query="options pattern validation validateonstart aspnet core")` |
| Scrutor decorator | `microsoft_docs_search(query="scrutor decorator pattern dotnet")` |
| Generic host DI | `microsoft_docs_search(query="dotnet generic host dependency injection worker service")` |
