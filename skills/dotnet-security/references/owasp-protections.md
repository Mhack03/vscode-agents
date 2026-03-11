# OWASP API Security Protections in .NET

---

## OWASP API Top 10 Checklist (2023)

| # | Risk | .NET Mitigation |
|---|------|-----------------|
| API1 | Broken Object Level Auth | `[Authorize]` + ownership check on every resource query |
| API2 | Broken Authentication | JWT `TokenValidationParameters`, refresh rotation, lockout |
| API3 | Object Property Exposure | Use DTOs/records — never expose EF entities directly |
| API4 | Unrestricted Resource Consumption | Rate limiting middleware + request size limits |
| API5 | Broken Function Level Auth | Authorization policies, role-based endpoint groups |
| API6 | Unrestricted Access to Sensitive Business Flow | Idempotency keys, CAPTCHA, account verification |
| API7 | Server-Side Request Forgery | Validate/whitelist URLs, disable `HttpClient` redirects |
| API8 | Security Misconfiguration | Security headers, HSTS, disable detailed errors in prod |
| API9 | Improper Inventory Management | API versioning with sunset headers |
| API10 | Unsafe API Consumption | Validate all third-party responses, use typed clients |

---

## Object-Level Authorization (API1)

```csharp
// ❌ Wrong — user can access ANY order by guessing ID
app.MapGet("/api/orders/{id}", async (int id, AppDbContext db) =>
    TypedResults.Ok(await db.Orders.FindAsync(id)));

// ✅ Correct — enforce ownership
app.MapGet("/api/orders/{id}", async (
    int id, AppDbContext db, ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var order  = await db.Orders
        .Where(o => o.Id == id && o.OwnerId == userId)
        .FirstOrDefaultAsync();

    return order is null
        ? TypedResults.NotFound()
        : TypedResults.Ok(order.ToDto());   // Always return DTO, not entity
});
```

---

## Mass Assignment Prevention (API3)

```csharp
// ❌ Never bind request body directly to entity
app.MapPost("/api/users/{id}", (int id, ApplicationUser user) =>
    /* ... */);

// ✅ Use a dedicated request DTO — only accept what you intend
public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string? Bio,
    string? AvatarUrl);

app.MapPut("/api/users/{id}/profile", async (
    int id, UpdateProfileRequest req, AppDbContext db, ClaimsPrincipal principal) =>
{
    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var user   = await db.Users.FindAsync(id);

    if (user is null || user.Id != userId)
        return TypedResults.Forbid();

    // Explicit property mapping — no UserRole, IsAdmin etc.
    user.FirstName = req.FirstName;
    user.LastName  = req.LastName;
    user.Bio       = req.Bio;
    user.AvatarUrl = req.AvatarUrl;
    await db.SaveChangesAsync();
    return TypedResults.NoContent();
});
```

---

## Rate Limiting (API4)

```csharp
builder.Services.AddRateLimiter(options =>
{
    // Strict limiter for auth endpoints
    options.AddFixedWindowLimiter("auth", cfg =>
    {
        cfg.PermitLimit         = 5;
        cfg.Window              = TimeSpan.FromMinutes(1);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit           = 0;
    });

    // Sliding window per user
    options.AddSlidingWindowLimiter("api", cfg =>
    {
        cfg.PermitLimit         = 100;
        cfg.Window              = TimeSpan.FromMinutes(1);
        cfg.SegmentsPerWindow   = 4;
    });

    options.RejectionStatusCode = 429;
    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.Headers.RetryAfter = "60";
        await ctx.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Title  = "Too Many Requests",
                Status = 429,
                Detail = "Rate limit exceeded. Please retry later.",
            }, ct);
    };
});

// Apply per-endpoint
auth.MapPost("/login", Handle).RequireRateLimiting("auth");
app.MapGroup("/api").RequireRateLimiting("api");
```

---

## Security Headers Middleware

```csharp
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"]           = "nosniff";
    headers["X-Frame-Options"]                  = "DENY";
    headers["X-XSS-Protection"]                 = "0";           // Disable legacy XSS; use CSP
    headers["Referrer-Policy"]                  = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"]               = "camera=(), microphone=(), geolocation=()";
    headers["Content-Security-Policy"]          =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';";

    // Remove identifying headers
    context.Response.Headers.Remove("Server");
    context.Response.Headers.Remove("X-Powered-By");
    context.Response.Headers.Remove("X-AspNet-Version");

    await next();
});

// HSTS — redirect HTTP to HTTPS
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
```

---

## CSRF Antiforgery (for cookie-based auth)

```csharp
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";  // SPA sends this header
});

app.UseAntiforgery();

// SPA token endpoint
app.MapGet("/antiforgery/token", (IAntiforgery af, HttpContext ctx) =>
{
    var tokens = af.GetAndStoreTokens(ctx);
    return TypedResults.Ok(new { token = tokens.RequestToken });
});
```

---

## SQL Injection Prevention

```csharp
// ✅ EF Core always parameterizes — safe by default
var products = await db.Products
    .Where(p => p.Name.Contains(searchTerm))
    .ToListAsync();

// ✅ Raw SQL — use parameters
var result = await db.Products
    .FromSqlInterpolated($"SELECT * FROM Products WHERE Name = {searchTerm}")
    .ToListAsync();

// ❌ Never do string concatenation with raw SQL
// db.Products.FromSqlRaw($"SELECT * FROM Products WHERE Name = '{userInput}'")
```

---

## Learn More

| Topic | Query |
|-------|-------|
| OWASP API security | `microsoft_docs_search(query="OWASP API Security Top 10 ASP.NET Core protection")` |
| Rate limiting | `microsoft_docs_search(query="ASP.NET Core rate limiting middleware fixed window sliding window")` |
| Security headers | `microsoft_docs_search(query="ASP.NET Core security headers CSP HSTS middleware")` |
