# API Versioning Strategies

---

## Strategy Comparison

| Method | URL Example | Pros | Cons |
|--------|-------------|------|------|
| URL Path | `/api/v1/products` | Obvious, cacheable, easy to test | Breaks REST purity |
| Query String | `/api/products?api-version=1.0` | Non-breaking | Easy to miss |
| Header | `api-version: 1.0` | Clean URLs | Harder to test in browser |
| Media Type | `Accept: application/vnd.myapi.v1+json` | True REST | Complex setup |

**Recommendation**: URL path for public APIs, header for internal/B2B.

---

## URL Path Versioning (Minimal API)

```csharp
// Program.cs
var v1 = app.MapGroup("/api/v1").WithOpenApi();
var v2 = app.MapGroup("/api/v2").WithOpenApi();

// v1 endpoint
v1.MapGet("/products", async (IProductServiceV1 svc) =>
    TypedResults.Ok(await svc.GetAllAsync()));

// v2 endpoint — breaking change (new response shape)
v2.MapGet("/products", async (IProductServiceV2 svc) =>
    TypedResults.Ok(await svc.GetPagedAsync()));
```

---

## Asp.Versioning (Header + Query String)

```bash
dotnet add package Asp.Versioning.Http
dotnet add package Asp.Versioning.Http.Client
```

```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion                  = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions                  = true;  // Adds api-supported-versions header
    options.ApiVersionReader = ApiVersionReader.Combine(
        new HeaderApiVersionReader("api-version"),
        new QueryStringApiVersionReader("api-version"));
});

// Endpoint declaration
var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .HasApiVersion(new ApiVersion(2, 0))
    .ReportApiVersions()
    .Build();

app.MapGet("/api/products", GetProducts)
   .WithApiVersionSet(versionSet)
   .MapToApiVersion(1, 0);

app.MapGet("/api/products", GetProductsV2)
   .WithApiVersionSet(versionSet)
   .MapToApiVersion(2, 0);
```

---

## Versioned OpenAPI Documents

```csharp
// Separate Scalar/Swagger page per version
builder.Services.AddOpenApi("v1");
builder.Services.AddOpenApi("v2");

app.MapOpenApi("/openapi/{documentName}.json");
app.MapScalarApiReference(opts =>
{
    opts.Servers = [];
    opts.AddPreferredSecuritySchemes("Bearer");
});
```

---

## Sunset / Deprecation Headers

Signal to clients that a version is going away:

```csharp
// Middleware to add Deprecation and Sunset headers on v1 responses
app.Use(async (ctx, next) =>
{
    await next();

    if (ctx.Request.Path.StartsWithSegments("/api/v1"))
    {
        ctx.Response.Headers["Deprecation"] = "true";
        ctx.Response.Headers["Sunset"]      =
            new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero)
                .ToString("R");                       // RFC 7231 date
        ctx.Response.Headers["Link"]        =
            "</api/v2/products>; rel=\"successor-version\"";
    }
});
```

---

## Non-Breaking vs Breaking Changes

```
Non-Breaking (no version bump needed):
✅ Add optional request field
✅ Add new response field
✅ Add a new endpoint
✅ Relax validation (allow more values)
✅ Add a new HTTP method to existing resource

Breaking (requires new version):
❌ Remove or rename request/response field
❌ Change field type (string → int)
❌ Change HTTP status codes
❌ Change URL structure
❌ Tighten validation (reject previously valid input)
❌ Remove an endpoint
```

---

## Learn More

| Topic | Query |
|-------|-------|
| Asp.Versioning | `microsoft_docs_search(query="Asp.Versioning.Http Minimal API versioning .NET 9")` |
| Sunset headers | `microsoft_docs_search(query="API deprecation sunset header RFC 8594")` |
| OpenAPI versioning | `microsoft_docs_search(query="ASP.NET Core OpenAPI multiple documents versioned Scalar")` |
