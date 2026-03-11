---
name: dotnet-api-design
description: REST API design standards for .NET 8/9/10 Minimal APIs — resource naming, HTTP semantics, pagination, filtering, versioning, idempotency, and API contracts.
license: Complete terms in LICENSE.txt
---

# .NET REST API Design Standards

## When to Use This Skill

- Designing new Minimal API endpoint routes and resource structure
- Choosing between URL versioning, header versioning, or query string versioning
- Implementing pagination (offset vs keyset/cursor)
- Adding consistent filtering and sorting to list endpoints
- Designing idempotent endpoints for payment/order flows
- Creating API contracts with OpenAPI before implementation (contract-first)

---

## Resource Naming Rules

```
✅ Correct
GET    /api/v1/products              List products
GET    /api/v1/products/42           Get product 42
POST   /api/v1/products              Create product
PUT    /api/v1/products/42           Replace product 42
PATCH  /api/v1/products/42           Partial update product 42
DELETE /api/v1/products/42           Delete product 42
GET    /api/v1/products/42/reviews   List reviews for product 42
POST   /api/v1/orders/42/cancel      Action on resource (verb as sub-resource)

❌ Incorrect
GET    /api/getProducts
POST   /api/createProduct
GET    /api/product/42               (singular)
DELETE /api/delete-product?id=42     (verb in path, operation in query)
```

---

## HTTP Status Code Contract

```
200 OK             — GET, PUT, PATCH success with response body
201 Created        — POST success; include Location header
204 No Content     — DELETE success; PUT with no body
400 Bad Request    — Validation failure (Problem Details with errors dict)
401 Unauthorized   — Missing or invalid token
403 Forbidden      — Authenticated but lacks permission
404 Not Found      — Resource does not exist
409 Conflict       — Duplicate / optimistic concurrency conflict
422 Unprocessable  — Business rule violation (not a validation error)
429 Too Many Req.  — Rate limit exceeded
500 Internal Error — Never leak stack traces; return Problem Details
```

---

## Pagination Patterns

### Offset Pagination (simple, use for admin UIs)

```
GET /api/v1/products?page=2&pageSize=20

Response:
{
  "items": [...],
  "totalCount": 150,
  "page": 2,
  "pageSize": 20,
  "totalPages": 8,
  "hasNext": true,
  "hasPrev": true
}
```

### Keyset / Cursor Pagination (use for high-volume, feeds)

```
GET /api/v1/events?limit=50&after=cursor_abc123

Response:
{
  "items": [...],
  "nextCursor": "cursor_xyz789",  // null if last page
  "hasMore": true
}
```

```csharp
// Keyset pagination — far more efficient than OFFSET for large tables
public async Task<CursorPagedResult<T>> GetPageAsync(
    string? afterCursor, int limit, CancellationToken ct)
{
    var afterId = DecodeCursor(afterCursor);  // Base64 decode → ID

    var items = await _context.Events
        .AsNoTracking()
        .Where(e => afterId == null || e.Id > afterId)
        .OrderBy(e => e.Id)
        .Take(limit + 1)  // Fetch one extra to check hasMore
        .Select(e => new EventDto(e.Id, e.Name, e.OccurredAt))
        .ToListAsync(ct);

    var hasMore = items.Count > limit;
    if (hasMore) items.RemoveAt(items.Count - 1);

    var nextCursor = hasMore ? EncodeCursor(items.Last().Id) : null;
    return new CursorPagedResult<T>(items, nextCursor, hasMore);
}
```

---

## Filtering & Sorting

```
GET /api/v1/products?status=active&minPrice=10&maxPrice=500&sort=price&order=asc

// Request model — query string binding
public record ProductFilter(
    string?        Status   = null,
    decimal?       MinPrice = null,
    decimal?       MaxPrice = null,
    string         Sort     = "name",
    string         Order    = "asc",
    int            Page     = 1,
    [Range(1,100)] int PageSize = 20);
```

```csharp
// Dynamic filtering
private static IQueryable<Product> ApplyFilter(
    IQueryable<Product> query, ProductFilter filter)
{
    if (filter.Status is not null)
        query = query.Where(p => p.Status == filter.Status);
    if (filter.MinPrice.HasValue)
        query = query.Where(p => p.Price >= filter.MinPrice.Value);
    if (filter.MaxPrice.HasValue)
        query = query.Where(p => p.Price <= filter.MaxPrice.Value);

    query = filter.Sort switch
    {
        "price"       => filter.Order == "desc"
                         ? query.OrderByDescending(p => p.Price)
                         : query.OrderBy(p => p.Price),
        "createdAt"   => filter.Order == "desc"
                         ? query.OrderByDescending(p => p.CreatedAt)
                         : query.OrderBy(p => p.CreatedAt),
        _             => query.OrderBy(p => p.Name)
    };

    return query;
}
```

---

## API Versioning

### URL Path Versioning (recommended for public APIs)

```csharp
// Route groups per version
var v1 = app.MapGroup("/api/v1");
var v2 = app.MapGroup("/api/v2");

v1.MapGet("/products", GetProductsV1);
v2.MapGet("/products", GetProductsV2);  // Breaking change: different response shape
```

### Header Versioning

```csharp
// Clients send: api-version: 2.0
builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
    o.ApiVersionReader = ApiVersionReader.Combine(
        new HeaderApiVersionReader("api-version"),
        new QueryStringApiVersionReader("api-version"));
});
```

---

## Idempotency Pattern

```
POST /api/v1/orders
Idempotency-Key: client-generated-uuid-here

// Server stores key → response mapping.
// Same key = same response (no duplicate order created).
```

---

## Consistent Error Envelope

All error responses must be `application/problem+json` (RFC 7807):

```json
{
	"type": "https://example.com/errors/product-not-found",
	"title": "Product not found",
	"status": 404,
	"detail": "Product with ID 42 does not exist.",
	"instance": "/api/v1/products/42",
	"traceId": "00-abc123-def456-00"
}
```

```csharp
// Use TypedResults.Problem or return right from exception handler
return TypedResults.Problem(
    detail:     $"Product with ID {id} does not exist.",
    statusCode: 404,
    title:      "Product not found",
    type:       "https://example.com/errors/product-not-found",
    instance:   ctx.Request.Path);
```

---

## References

| Topic                                                        | Load When                                                      |
| ------------------------------------------------------------ | -------------------------------------------------------------- |
| [Versioning Strategies](references/versioning-strategies.md) | Deprecation, sunset headers, multi-version OpenAPI docs        |
| [Contract-First Design](references/contract-first.md)        | OpenAPI-first workflow, code generation from spec, API mocking |

## Learn More

| Topic               | Query                                                                                         |
| ------------------- | --------------------------------------------------------------------------------------------- |
| Minimal API routing | `microsoft_docs_search(query="ASP.NET Core Minimal API route groups versioning .NET 9")`      |
| Problem Details     | `microsoft_docs_search(query="ASP.NET Core Problem Details IProblemDetailsService RFC 7807")` |
| API versioning      | `microsoft_docs_search(query="ASP.NET Core API versioning Asp.Versioning.Http .NET 9")`       |
