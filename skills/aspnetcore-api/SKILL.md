---
name: aspnetcore-api
description: ASP.NET Core Web API patterns and best practices for .NET 8/9/10. Use when creating REST APIs, minimal APIs, API controllers, route groups, endpoint filters, API versioning, OpenAPI/Scalar/Swagger documentation, Problem Details RFC 7807 error responses, IResult TypedResults, rate limiting middleware, output caching, health checks, IExceptionHandler, global exception handling, CORS configuration, or middleware pipeline ordering in C# ASP.NET Core projects.
license: Complete terms in LICENSE.txt
---

# ASP.NET Core Web API Patterns

Production patterns for building .NET 8/9/10 Web APIs covering minimal APIs, controllers, error handling, performance, and documentation.

## When to Use This Skill

- Building or extending ASP.NET Core REST API endpoints
- Choosing between Minimal APIs and Controller-based APIs
- Returning typed HTTP results (`IResult`, `TypedResults`)
- Setting up OpenAPI/Scalar/Swagger documentation
- Handling errors with Problem Details (RFC 7807)
- Applying endpoint filters for validation, logging, or auth
- Configuring rate limiting, output caching, or health checks
- Setting up API versioning
- Configuring CORS for React+Vite frontend integration

## Prerequisites

```bash
dotnet new webapi -n MyApp.Api --use-minimal-apis   # .NET 9 default
dotnet add package Asp.Versioning.Http               # API versioning
dotnet add package Microsoft.AspNetCore.OpenApi      # Built-in (.NET 9+)
```

## Minimal APIs vs Controllers

| Aspect   | Minimal APIs                | Controllers                  |
| -------- | --------------------------- | ---------------------------- |
| Ceremony | Low                         | Higher                       |
| Routing  | Route groups                | Attribute routing            |
| Filters  | `IEndpointFilter`           | Action filters               |
| Best For | New services, microservices | Large teams, complex routing |
| Testing  | `WebApplicationFactory`     | Same                         |

## Program.cs — Recommended Structure

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. OpenAPI / docs
builder.Services.AddOpenApi();

// 2. Problem Details
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// 3. Performance
builder.Services.AddOutputCache();
builder.Services.AddRateLimiter(o => o.AddFixedWindowLimiter("api", opt =>
{
    opt.PermitLimit = 100;
    opt.Window = TimeSpan.FromMinutes(1);
}));

// 4. Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("db");

// 5. CORS (React + Vite dev)
builder.Services.AddCors(o => o.AddPolicy("vite", p =>
    p.WithOrigins("http://localhost:5173", "https://localhost:5173")
     .AllowAnyMethod().AllowAnyHeader().AllowCredentials()));

// 6. App services
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Middleware — ORDER MATTERS
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("vite");
app.UseOutputCache();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();           // GET /openapi/v1.json

app.MapHealthChecks("/health");

// Route groups
var api = app.MapGroup("/api/v{version:apiVersion}")
             .WithOpenApi();

api.MapProductEndpoints();
api.MapOrderEndpoints();

app.Run();
```

## Route Groups Pattern

```csharp
public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(
        this RouteGroupBuilder parent)
    {
        var group = parent.MapGroup("/products")
            .WithTags("Products")
            .RequireRateLimiting("api");

        group.MapGet("/", GetAll)
             .CacheOutput(p => p.Expire(TimeSpan.FromMinutes(5)).Tag("products"));
        group.MapGet("/{id:int}", GetById).WithName("GetProductById");
        group.MapPost("/", Create).RequireAuthorization();
        group.MapPut("/{id:int}", Update).RequireAuthorization();
        group.MapDelete("/{id:int}", Delete).RequireAuthorization("admin");

        return group;
    }

    // Typed results give compile-time safety — prefer over Results.Ok(...)
    static async Task<Ok<IReadOnlyList<ProductDto>>> GetAll(
        IProductService svc, CancellationToken ct) =>
        TypedResults.Ok(await svc.GetAllAsync(ct));

    static async Task<Results<Ok<ProductDto>, NotFound>> GetById(
        int id, IProductService svc, CancellationToken ct) =>
        await svc.GetByIdAsync(id, ct) is { } p
            ? TypedResults.Ok(p)
            : TypedResults.NotFound();

    static async Task<Results<Created<ProductDto>, BadRequest<ProblemDetails>>> Create(
        CreateProductRequest req,
        IProductService svc,
        LinkGenerator links,
        CancellationToken ct)
    {
        var result = await svc.CreateAsync(req, ct);
        if (!result.IsSuccess)
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title  = "Validation Failed",
                Detail = result.Error,
                Status = 400
            });

        var location = links.GetPathByName("GetProductById",
            new { id = result.Value!.Id });
        return TypedResults.Created(location, result.Value);
    }
}
```

## Problem Details (RFC 7807) — Global Exception Handler

```csharp
public class GlobalExceptionHandler(IProblemDetailsService problemDetails)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            NotFoundException       => (404, "Not Found"),
            ValidationException     => (400, "Validation Failed"),
            UnauthorizedAccessException => (403, "Forbidden"),
            _                       => (500, "Internal Server Error")
        };

        ctx.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext     = ctx,
            Exception       = ex,
            ProblemDetails  = { Title = title, Status = status, Detail = ex.Message }
        });
    }
}
```

See [endpoint-filters.md](references/endpoint-filters.md) for validation filters.
See [openapi-scalar.md](references/openapi-scalar.md) for docs setup.
See [security-auth.md](references/security-auth.md) for JWT, OAuth2, and authorization policies.

## Quick API Reference

| Return Type              | Factory                                 | Status |
| ------------------------ | --------------------------------------- | ------ |
| `Ok<T>`                  | `TypedResults.Ok(value)`                | 200    |
| `Created<T>`             | `TypedResults.Created(location, value)` | 201    |
| `NoContent`              | `TypedResults.NoContent()`              | 204    |
| `BadRequest<T>`          | `TypedResults.BadRequest(problem)`      | 400    |
| `NotFound`               | `TypedResults.NotFound()`               | 404    |
| `UnauthorizedHttpResult` | `TypedResults.Unauthorized()`           | 401    |
| `ForbidHttpResult`       | `TypedResults.Forbid()`                 | 403    |

## Learn More

| Topic                 | How to Find                                                                                               |
| --------------------- | --------------------------------------------------------------------------------------------------------- |
| Minimal APIs overview | `microsoft_docs_search(query="aspnet core minimal api overview .net 9")`                                  |
| Problem Details       | `microsoft_docs_search(query="problem details RFC 7807 aspnet core IExceptionHandler")`                   |
| Output caching        | `microsoft_docs_search(query="output caching middleware aspnet core")`                                    |
| Rate limiting         | `microsoft_docs_search(query="rate limiting middleware aspnet core fixed window")`                        |
| API versioning        | `microsoft_docs_search(query="aspnet core api versioning Asp.Versioning.Http")`                           |
| OpenAPI .NET 9        | `microsoft_docs_fetch(url="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview")` |
| Health checks         | `microsoft_docs_search(query="aspnet core health checks IHealthCheck")`                                   |
| TypedResults          | `microsoft_docs_search(query="aspnet core TypedResults IResult minimal api")`                             |
