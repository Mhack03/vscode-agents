# Endpoint Filters & Validation — ASP.NET Core

Endpoint filters are the Minimal API equivalent of action filters in MVC. They execute before and after endpoint handlers.

---

## IEndpointFilter Lifecycle

```
Request → Filter 1 Before → Filter 2 Before → Handler → Filter 2 After → Filter 1 After → Response
```

Filters on a group apply to every endpoint in that group. Filters on `app` apply globally.

---

## Validation Filter (FluentValidation)

```csharp
// Filters/ValidationFilter.cs
public class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next)
    {
        var arg = ctx.Arguments.OfType<T>().FirstOrDefault();

        if (arg is null)
            return TypedResults.BadRequest("Request body is required.");

        var result = await validator.ValidateAsync(arg, ctx.HttpContext.RequestAborted);

        if (!result.IsValid)
            return TypedResults.ValidationProblem(result.ToDictionary());

        return await next(ctx);
    }
}

// Apply to a single endpoint
orders.MapPost("/", CreateOrder)
      .AddEndpointFilter<ValidationFilter<CreateOrderRequest>>();

// Apply to entire group
var products = app.MapGroup("/api/products")
    .AddEndpointFilter<ValidationFilter<CreateProductRequest>>();
```

---

## Validation Filter (DataAnnotations — no extra package)

```csharp
public class DataAnnotationsValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next)
    {
        foreach (var arg in ctx.Arguments)
        {
            if (arg is null) continue;

            var validationContext = new ValidationContext(arg);
            var errors = new List<ValidationResult>();

            if (!Validator.TryValidateObject(arg, validationContext, errors, true))
            {
                var dict = errors
                    .GroupBy(e => e.MemberNames.FirstOrDefault() ?? "general")
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage ?? "Invalid").ToArray());

                return TypedResults.ValidationProblem(dict);
            }
        }

        return await next(ctx);
    }
}
```

---

## Logging Filter

```csharp
public class RequestLoggingFilter(ILogger<RequestLoggingFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next)
    {
        var endpoint = ctx.HttpContext.GetEndpoint()?.DisplayName;
        logger.LogInformation("Executing {Endpoint}", endpoint);

        var sw = Stopwatch.StartNew();
        var result = await next(ctx);
        sw.Stop();

        logger.LogInformation("Completed {Endpoint} in {Elapsed}ms",
            endpoint, sw.ElapsedMilliseconds);

        return result;
    }
}
```

---

## Idempotency Key Filter

```csharp
// Prevents duplicate requests using a client-supplied key header
public class IdempotencyFilter(IDistributedCache cache) : IEndpointFilter
{
    private const string Header = "Idempotency-Key";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next)
    {
        var idempotencyKey = ctx.HttpContext.Request.Headers[Header].FirstOrDefault();

        if (string.IsNullOrEmpty(idempotencyKey))
            return TypedResults.BadRequest($"Header '{Header}' is required.");

        var cacheKey = $"idempotency:{idempotencyKey}";
        var cached = await cache.GetStringAsync(cacheKey);

        if (cached is not null)
        {
            ctx.HttpContext.Response.Headers["X-Idempotency-Replayed"] = "true";
            return Results.Json(JsonSerializer.Deserialize<object>(cached));
        }

        var result = await next(ctx);

        if (result is IValueHttpResult value)
        {
            var json = JsonSerializer.Serialize(value.Value);
            await cache.SetStringAsync(cacheKey, json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                });
        }

        return result;
    }
}
```

---

## Combining Multiple Filters

```csharp
// Filters are executed in registration order
app.MapPost("/api/orders", CreateOrder)
   .AddEndpointFilter<RequestLoggingFilter>()        // runs first
   .AddEndpointFilter<IdempotencyFilter>()
   .AddEndpointFilter<ValidationFilter<CreateOrderRequest>>()  // runs last before handler
   .RequireAuthorization("orders:write");
```

---

## Route Group Filters

```csharp
// Apply common filters to an entire group
var api = app.MapGroup("/api")
    .AddEndpointFilter<RequestLoggingFilter>()
    .RequireAuthorization();

var v1 = api.MapGroup("/v1");

// Nested group inherits parent filters + adds its own
var products = v1.MapGroup("/products")
    .WithTags("Products")
    .AddEndpointFilter<ValidationFilter<CreateProductRequest>>();
```

---

## Learn More

| Topic | Query |
|-------|-------|
| Endpoint filters | `microsoft_docs_search(query="aspnet core minimal api endpoint filter IEndpointFilter")` |
| FluentValidation integration | `microsoft_docs_search(query="FluentValidation aspnet core minimal api")` |
| Filter factory | `microsoft_docs_search(query="aspnet core endpoint filter factory short circuit")` |
