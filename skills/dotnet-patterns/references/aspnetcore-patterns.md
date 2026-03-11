# ASP.NET Core Patterns

Reference for ASP.NET Core 8/9/10 Web API, Blazor, and middleware patterns.

---

## Minimal APIs vs Controller-Based APIs

### Minimal APIs (Recommended for new projects)

Pros: Less ceremony, faster startup, inline lambdas or handler classes, good for microservices.

```csharp
// Endpoint groups — keeps Program.cs clean
app.MapOrderEndpoints();

// Extension method grouping
public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders")
            .RequireAuthorization();

        group.MapGet("/",          GetAll);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/",         Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);

        return app;
    }

    static async Task<IResult> GetAll(
        IOrderService svc, CancellationToken ct) =>
        Results.Ok(await svc.GetAllAsync(ct));

    static async Task<IResult> GetById(
        Guid id, IOrderService svc, CancellationToken ct) =>
        await svc.GetByIdAsync(id, ct) is { } order
            ? Results.Ok(order)
            : Results.NotFound();

    static async Task<IResult> Create(
        CreateOrderRequest req, IOrderService svc, CancellationToken ct)
    {
        var result = await svc.CreateOrderAsync(req, ct);
        return result.IsSuccess
            ? Results.Created($"/api/orders/{result.Value!.Id}", result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    static async Task<IResult> Update(
        Guid id, UpdateOrderRequest req, IOrderService svc, CancellationToken ct) =>
        await svc.UpdateAsync(id, req, ct)
            ? Results.NoContent()
            : Results.NotFound();

    static async Task<IResult> Delete(
        Guid id, IOrderService svc, CancellationToken ct) =>
        await svc.DeleteAsync(id, ct)
            ? Results.NoContent()
            : Results.NotFound();
}
```

### Controller-Based APIs

Prefer when: team is familiar with MVC, you use global filters, or migrating existing code.

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService svc) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetAll(
        CancellationToken ct) =>
        Ok(await svc.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(
        Guid id, CancellationToken ct)
    {
        var order = await svc.GetByIdAsync(id, ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(
        CreateOrderRequest req, CancellationToken ct)
    {
        var result = await svc.CreateOrderAsync(req, ct);
        if (!result.IsSuccess)
            return Problem(result.Error, statusCode: 400);

        return CreatedAtAction(nameof(GetById),
            new { id = result.Value!.Id }, result.Value);
    }
}
```

---

## Problem Details (RFC 7807)

Return standardised error responses from all endpoints:

```csharp
// Program.cs — enables ProblemDetails globally
builder.Services.AddProblemDetails();

// In minimal APIs
return Results.Problem(
    title:      "Validation failed",
    detail:     "Order must have at least one line.",
    statusCode: StatusCodes.Status400BadRequest,
    extensions: new Dictionary<string, object?> { ["traceId"] = Activity.Current?.Id });

// In controllers — ControllerBase.Problem() helper
return Problem(
    title:      "Order not found",
    statusCode: StatusCodes.Status404NotFound);
```

### Global Exception Handler (Preferred over try/catch everywhere)

```csharp
// Register
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
app.UseExceptionHandler();

// Implementation
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        logger.LogError(exception, "Unhandled exception");

        var problem = new ProblemDetails
        {
            Status   = StatusCodes.Status500InternalServerError,
            Title    = "An unexpected error occurred.",
            Detail   = exception.Message,  // Omit in production
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = problem.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
```

---

## Endpoint Filters (Minimal API Middleware)

Apply cross-cutting logic (validation, logging, auth) to individual endpoints or groups:

```csharp
// Validation filter using FluentValidation or DataAnnotations
public class ValidationFilter<TRequest> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next)
    {
        var req = ctx.GetArgument<TRequest>(0);

        if (req is null)
            return Results.BadRequest("Request body required.");

        var validationContext = new ValidationContext(req);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(req, validationContext, results, true))
        {
            return Results.ValidationProblem(
                results.ToDictionary(
                    r => r.MemberNames.FirstOrDefault() ?? "general",
                    r => new[] { r.ErrorMessage ?? "Invalid" }));
        }

        return await next(ctx);
    }
}

// Apply to a group
var orders = app.MapGroup("/api/orders")
    .AddEndpointFilter<ValidationFilter<CreateOrderRequest>>();
```

---

## Authentication & Authorization

### JWT Bearer Authentication

```csharp
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        var jwt = builder.Configuration.GetSection("JwtSettings")
                         .Get<JwtSettings>()!;

        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwt.Issuer,
            ValidAudience            = jwt.Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwt.Secret))
        };
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    o.AddPolicy("CanManageOrders", p =>
        p.RequireAuthenticatedUser()
         .RequireClaim("permissions", "orders:write"));
});

// Middleware order — must come before UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

// Apply to endpoint
orders.MapPost("/", Create)
      .RequireAuthorization("CanManageOrders");

// Or allow anonymous on specific endpoint in protected group
group.MapGet("/public", GetPublic).AllowAnonymous();
```

### Token Generation

```csharp
public class JwtTokenService(IOptions<JwtSettings> options)
{
    private readonly JwtSettings _jwt = options.Value;

    public string GenerateToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email,          user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _jwt.Issuer,
            audience:           _jwt.Audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

---

## Middleware Pipeline

Order matters — requests pass top-down, responses bottom-up:

```csharp
var app = builder.Build();

// 1. Exception handling (first — catches everything below)
app.UseExceptionHandler();

// 2. HTTPS Redirection
app.UseHttpsRedirection();

// 3. Static files (before routing in Blazor hosts)
app.UseStaticFiles();

// 4. Routing
app.UseRouting();

// 5. CORS (after UseRouting, before UseAuth)
app.UseCors("AllowVite");

// 6. Authentication (verifies who they are)
app.UseAuthentication();

// 7. Authorization (verifies what they can do)
app.UseAuthorization();

// 8. Endpoints
app.MapControllers();           // or
app.MapOrderEndpoints();        // minimal API groups
app.MapBlazorHub();             // Blazor Server
app.MapFallbackToPage("/_Host"); // Blazor Server fallback
```

### Custom Middleware

```csharp
// Inline middleware (simple cases)
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Request-Id"] = Guid.NewGuid().ToString();
    await next(ctx);
});

// Class-based middleware (preferred for complex cases)
public class RequestTimingMiddleware(RequestDelegate next,
    ILogger<RequestTimingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        var sw = Stopwatch.StartNew();
        await next(ctx);
        sw.Stop();
        logger.LogInformation("{Method} {Path} completed in {Ms}ms",
            ctx.Request.Method, ctx.Request.Path, sw.ElapsedMilliseconds);
    }
}

// Extension method
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseRequestTiming(
        this IApplicationBuilder app) =>
        app.UseMiddleware<RequestTimingMiddleware>();
}

app.UseRequestTiming();
```

---

## CORS (for React+Vite Frontend)

```csharp
// Program.cs
builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowVite", p =>
        p.WithOrigins(
            "http://localhost:5173",   // Vite default
            "https://localhost:5173")
         .AllowAnyMethod()
         .AllowAnyHeader()
         .AllowCredentials());         // Required for cookies/auth headers

    o.AddPolicy("ProductionCors", p =>
        p.WithOrigins("https://myapp.example.com")
         .WithMethods("GET", "POST", "PUT", "DELETE")
         .WithHeaders("Content-Type", "Authorization"));
});

// Apply correct policy per environment
app.UseCors(app.Environment.IsDevelopment() ? "AllowVite" : "ProductionCors");
```

---

## Health Checks

```csharp
// Registration
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database")
    .AddUrlGroup(new Uri("https://api.stripe.com"), "stripe")
    .AddCheck<CustomHealthCheck>("custom");

// Map endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

---

## OpenAPI / Swagger Setup (.NET 9+ built-in)

```csharp
// .NET 9 uses Microsoft.AspNetCore.OpenApi (no Swashbuckle needed)
builder.Services.AddOpenApi();

app.MapOpenApi();  // Serves /openapi/v1.json

// Scalar UI (modern alternative to Swagger UI)
app.MapScalarApiReference();

// Add metadata to endpoints
orders.MapPost("/", Create)
    .WithName("CreateOrder")
    .WithSummary("Create a new order")
    .WithDescription("Creates an order and returns 201 with the created resource.")
    .Produces<OrderDto>(201)
    .ProducesProblem(400)
    .ProducesProblem(401);
```

---

## Blazor Integration Patterns

### Blazor Auto Render Mode (.NET 8+)

```csharp
// Program.cs
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Client.Pages.Counter).Assembly);
```

```razor
@* Global render mode in App.razor *@
<Routes @rendermode="InteractiveAuto" />

@* Per-component override *@
@rendermode InteractiveServer
```

### Calling .NET API from Blazor WASM

```csharp
// Program.cs (Client WASM project)
builder.Services.AddHttpClient("Api", client =>
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

// In component
@inject IHttpClientFactory HttpFactory

var http = HttpFactory.CreateClient("Api");
var orders = await http.GetFromJsonAsync<List<OrderDto>>("/api/orders");
```

---

## Learn More

| Topic | Search |
|-------|--------|
| Minimal API overview | `microsoft_docs_fetch(url="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/overview")` |
| Endpoint filters | `microsoft_docs_search(query="aspnet core minimal api endpoint filter")` |
| JWT authentication | `microsoft_docs_search(query="aspnet core jwt bearer token authentication")` |
| Blazor render modes | `microsoft_docs_search(query="blazor net8 render mode interactive server wasm auto")` |
| OpenAPI .NET 9 | `microsoft_docs_search(query="aspnet core openapi net9 addopenapi")` |
| Scalar UI | `microsoft_docs_search(query="aspnet core scalar openapi ui")` |
| Health checks | `microsoft_docs_search(query="aspnet core health checks iHealthCheck")` |
