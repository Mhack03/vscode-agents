---
name: dotnet-patterns
description: C# and .NET 8/9/10 development patterns and best practices. Use when creating, modifying, or reviewing .NET applications including ASP.NET Core Web APIs, Blazor, console apps, or class libraries. Covers C# 12/13 language features (records, primary constructors, pattern matching, collection expressions), dependency injection, async/await, nullable reference types, Options pattern, repository pattern, Result pattern, middleware pipeline, and minimal APIs. Use when building .NET backends that pair with React+Vite+TypeScript or Blazor frontends.
license: Complete terms in LICENSE.txt
---

# .NET Patterns

Production-ready C# and .NET 8/9/10 patterns covering modern language features, architectural patterns, and best practices for building robust APIs, Blazor apps, and class libraries.

## When to Use This Skill

- Creating or scaffolding a .NET 8/9/10 project
- Writing ASP.NET Core Web API endpoints (minimal APIs or controllers)
- Implementing dependency injection, `IOptions<T>`, or service lifetimes
- Using C# 12/13 features: records, primary constructors, pattern matching, collection expressions
- Applying async/await correctly (avoiding deadlocks, `ConfigureAwait`, cancellation)
- Implementing repository pattern with Entity Framework Core
- Setting up middleware, filters, or request pipeline customization
- Handling errors with Result/Problem Details pattern
- Pairing a .NET API with a React+Vite+TypeScript or Blazor frontend

## Prerequisites

- .NET SDK 8, 9, or 10 (`dotnet --version`)
- IDE: Visual Studio 2022 17.8+ or VS Code with C# Dev Kit
- EF Core CLI (optional): `dotnet tool install -g dotnet-ef`

## Project Structure

```
MyApp/
├── MyApp.Api/               # ASP.NET Core Web API or Blazor host
│   ├── Program.cs           # Entry point, DI registration, middleware
│   ├── appsettings.json     # Configuration
│   └── Endpoints/           # Minimal API endpoint groups
├── MyApp.Application/       # Business logic, use cases, DTOs
│   ├── Features/            # Feature folders (CQRS-style)
│   └── Common/              # Shared abstractions
├── MyApp.Domain/            # Entities, value objects, domain rules
│   ├── Entities/
│   └── Interfaces/          # Repository contracts
├── MyApp.Infrastructure/    # EF Core, external services, repositories
│   ├── Data/                # DbContext, migrations
│   └── Repositories/
└── MyApp.Tests/             # xUnit, NUnit, or MSTest test project
```

## C# 12/13 Language Features

### Primary Constructors

Declare constructor parameters directly on the class (or record) declaration:

```csharp
// Class with primary constructor — parameters are in scope throughout the class body
public class OrderService(IOrderRepository repo, ILogger<OrderService> logger)
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        logger.LogInformation("Fetching order {Id}", id);
        return await repo.GetByIdAsync(id, ct);
    }
}
```

### Records

Use `record` for immutable data transfer objects and value objects:

```csharp
// Positional record — compiler generates constructor, Equals, GetHashCode, ToString
public record CreateOrderRequest(Guid CustomerId, List<OrderLineDto> Lines);

// Record with validation via property init
public record OrderLineDto
{
    public required Guid ProductId { get; init; }
    public required int Quantity { get; init; }
}
```

### Pattern Matching

```csharp
string Classify(object obj) => obj switch
{
    int n when n < 0   => "negative",
    int n when n == 0  => "zero",
    int                => "positive",
    string { Length: 0 } => "empty string",
    string s           => $"string: {s}",
    null               => "null",
    _                  => "unknown"
};
```

### Collection Expressions (C# 12)

```csharp
int[] numbers    = [1, 2, 3, 4, 5];
List<string> ids = ["a", "b", "c"];
Span<byte>  raw  = [0x0A, 0x0B];

// Spread operator
int[] combined = [..numbers, 6, 7];
```

### Nullable Reference Types

Enable in `.csproj` (default in .NET 8+):

```xml
<Nullable>enable</Nullable>
```

```csharp
// Compiler warns when null flows into non-nullable references
public string Name { get; set; } = string.Empty;  // Non-nullable, must init
public string? Nickname { get; set; }              // Nullable, fine as null
```

## Core .NET Patterns

### Dependency Injection & Service Lifetimes

Register services in `Program.cs`:

```csharp
builder.Services.AddScoped<IOrderRepository, OrderRepository>();   // Per HTTP request
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();    // New per injection
builder.Services.AddSingleton<ICacheService, MemoryCacheService>(); // App lifetime
```

> **Rule**: Never inject `Scoped` into `Singleton` — use `IServiceScopeFactory` instead.

See [dependency-injection.md](references/dependency-injection.md) for advanced DI patterns.

### Options Pattern (`IOptions<T>`)

```csharp
// 1. Define a settings class
public class JwtSettings
{
    public required string Issuer   { get; init; }
    public required string Audience { get; init; }
    public required string Secret   { get; init; }
}

// 2. Bind in Program.cs
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

// 3. Inject where needed
public class TokenService(IOptions<JwtSettings> options)
{
    private readonly JwtSettings _jwt = options.Value;
}
```

See full code: [sample_codes/common-patterns/options-pattern.cs](sample_codes/common-patterns/options-pattern.cs)

### Repository Pattern with EF Core

```csharp
// Contract in Domain layer
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default);
    void Add(Order order);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

// Implementation in Infrastructure layer
public class OrderRepository(AppDbContext db) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default) =>
        await db.Orders.AsNoTracking().ToListAsync(ct);

    public void Add(Order order) => db.Orders.Add(order);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
```

See full code: [sample_codes/common-patterns/repository-pattern.cs](sample_codes/common-patterns/repository-pattern.cs)

### Result Pattern (Error Handling without Exceptions)

```csharp
public readonly record struct Result<T>
{
    public T? Value       { get; }
    public string? Error  { get; }
    public bool IsSuccess { get; }

    private Result(T value)           { Value = value; IsSuccess = true; }
    private Result(string error)      { Error = error; IsSuccess = false; }

    public static Result<T> Ok(T value)       => new(value);
    public static Result<T> Fail(string error) => new(error);
}

// Usage
public async Task<Result<Order>> CreateOrderAsync(CreateOrderRequest req)
{
    if (!req.Lines.Any())
        return Result<Order>.Fail("Order must have at least one line.");

    var order = new Order(req.CustomerId, req.Lines);
    _repo.Add(order);
    await _repo.SaveChangesAsync();
    return Result<Order>.Ok(order);
}
```

See full code: [sample_codes/common-patterns/result-pattern.cs](sample_codes/common-patterns/result-pattern.cs)

## Async/Await Best Practices

```csharp
// ✅ Always propagate CancellationToken
public async Task<List<Product>> GetProductsAsync(CancellationToken ct = default)
    => await _db.Products.ToListAsync(ct);

// ✅ Use ConfigureAwait(false) in library/infrastructure code
public async Task<string> ReadFileAsync(string path)
{
    using var reader = File.OpenText(path);
    return await reader.ReadToEndAsync().ConfigureAwait(false);
}

// ✅ Return Task directly when just forwarding (no using/try/finally needed)
public Task<Order?> FindAsync(Guid id) => _repo.GetByIdAsync(id);

// ❌ Avoid — blocks the thread, causes deadlocks in ASP.NET
var result = SomeAsyncMethod().Result;
var result = SomeAsyncMethod().GetAwaiter().GetResult();

// ❌ Avoid async void (swallows exceptions); use async Task
// async void OnButtonClick() { ... } — only valid for event handlers
```

See [sample_codes/common-patterns/async-patterns.cs](sample_codes/common-patterns/async-patterns.cs) for `ValueTask`, parallel tasks, and `IAsyncEnumerable<T>`.

## Minimal API Quick Reference

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

// Route group with prefix
var orders = app.MapGroup("/api/orders").WithTags("Orders");

orders.MapGet("/",      async (IOrderRepository repo, CancellationToken ct) =>
    Results.Ok(await repo.GetAllAsync(ct)));

orders.MapGet("/{id:guid}", async (Guid id, IOrderRepository repo, CancellationToken ct) =>
    await repo.GetByIdAsync(id, ct) is { } order
        ? Results.Ok(order)
        : Results.NotFound());

orders.MapPost("/", async (CreateOrderRequest req, IOrderService svc, CancellationToken ct) =>
{
    var result = await svc.CreateOrderAsync(req, ct);
    return result.IsSuccess
        ? Results.Created($"/api/orders/{result.Value!.Id}", result.Value)
        : Results.BadRequest(result.Error);
});

app.Run();
```

See full: [sample_codes/getting-started/minimal-webapi.cs](sample_codes/getting-started/minimal-webapi.cs)

## Program.cs Structure (Recommended Order)

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// 2. Infrastructure
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// 3. Application services
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

// 4. API layer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("https://localhost:5173")  // Vite dev server
     .AllowAnyMethod()
     .AllowAnyHeader()));

var app = builder.Build();

// 5. Middleware (order matters)
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// 6. Map endpoints
app.MapOrderEndpoints();  // extension method grouping
app.Run();
```

See [aspnetcore-patterns.md](references/aspnetcore-patterns.md) for middleware, filters, and auth patterns.

## Learn More

| Topic                        | How to Find                                                                                                 |
| ---------------------------- | ----------------------------------------------------------------------------------------------------------- |
| EF Core migrations & queries | `microsoft_docs_search(query="entity framework core 8 migrations query")`                                   |
| Blazor component lifecycle   | `microsoft_docs_search(query="blazor component lifecycle oninitialized")`                                   |
| ASP.NET Core auth & JWT      | `microsoft_docs_search(query="aspnet core jwt bearer authentication 8")`                                    |
| `IAsyncEnumerable` streaming | `microsoft_docs_search(query="C# IAsyncEnumerable streaming asp net core")`                                 |
| CQRS with MediatR            | `microsoft_docs_search(query="mediatr cqrs asp net core")`                                                  |
| Background services          | `microsoft_docs_search(query="asp net core background service hosted service")`                             |
| Health checks                | `microsoft_docs_search(query="asp net core health checks")`                                                 |
| Minimal API filters          | `microsoft_docs_search(query="minimal api endpoint filter aspnet core")`                                    |
| xUnit testing Web API        | `microsoft_docs_search(query="xunit webapplicationfactory integration testing")`                            |
| C# 12/13 full spec           | `microsoft_docs_fetch(url="https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12")`           |
| .NET dependency injection    | `microsoft_docs_fetch(url="https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection")` |
| Code samples                 | `microsoft_code_sample_search(query="aspnet core minimal api", language="csharp")`                          |
