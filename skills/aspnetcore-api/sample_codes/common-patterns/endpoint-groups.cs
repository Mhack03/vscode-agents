// aspnetcore-api/sample_codes/common-patterns/endpoint-groups.cs
// Complete Minimal API endpoint group with filters, TypedResults, route groups, and versioning

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OutputCaching;

namespace MyApi.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        // Versioned route group — v1
        var v1 = app.MapGroup("/api/v1/products")
            .WithTags("Products")
            .RequireAuthorization()
            .AddEndpointFilter<RequestLoggingFilter>();

        v1.MapGet("/", GetAll)
            .AllowAnonymous()
            .CacheOutput("Products")
            .WithSummary("List all products")
            .Produces<PagedResult<ProductDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        v1.MapGet("/{id:int}", GetById)
            .AllowAnonymous()
            .CacheOutput(b => b.Expire(TimeSpan.FromMinutes(10)).Tag("products"))
            .WithName("GetProductById")
            .Produces<ProductDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        v1.MapPost("/", Create)
            .RequireAuthorization("CanWriteProducts")
            .AddEndpointFilter<ValidationFilter<CreateProductRequest>>()
            .WithSummary("Create product")
            .Produces<ProductDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        v1.MapPut("/{id:int}", Update)
            .RequireAuthorization("CanWriteProducts")
            .AddEndpointFilter<ValidationFilter<UpdateProductRequest>>()
            .Produces<ProductDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        v1.MapDelete("/{id:int}", Delete)
            .RequireAuthorization("AdminOnly")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    // GET /api/v1/products?page=1&pageSize=20&search=widget
    private static async Task<Ok<PagedResult<ProductDto>>> GetAll(
        IProductService svc,
        int page = 1, int pageSize = 20, string? search = null,
        CancellationToken ct = default)
    {
        var result = await svc.GetPagedAsync(page, pageSize, search, ct);
        return TypedResults.Ok(result);
    }

    // GET /api/v1/products/42
    private static async Task<Results<Ok<ProductDto>, NotFound>> GetById(
        int id,
        IProductService svc,
        CancellationToken ct)
    {
        var product = await svc.GetByIdAsync(id, ct);
        return product is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(product);
    }

    // POST /api/v1/products
    private static async Task<Results<CreatedAtRoute<ProductDto>, ValidationProblem>> Create(
        CreateProductRequest request,
        IProductService svc,
        IOutputCacheStore cache,
        CancellationToken ct)
    {
        var product = await svc.CreateAsync(request, ct);

        // Invalidate cached product lists
        await cache.EvictByTagAsync("products", ct);

        return TypedResults.CreatedAtRoute(product, "GetProductById",
            new { id = product.Id });
    }

    // PUT /api/v1/products/42
    private static async Task<Results<Ok<ProductDto>, NotFound>> Update(
        int id,
        UpdateProductRequest request,
        IProductService svc,
        IOutputCacheStore cache,
        CancellationToken ct)
    {
        var product = await svc.UpdateAsync(id, request, ct);

        if (product is null) return TypedResults.NotFound();

        await cache.EvictByTagAsync("products", ct);
        return TypedResults.Ok(product);
    }

    // DELETE /api/v1/products/42
    private static async Task<Results<NoContent, NotFound>> Delete(
        int id,
        IProductService svc,
        IOutputCacheStore cache,
        CancellationToken ct)
    {
        var deleted = await svc.DeleteAsync(id, ct);

        if (!deleted) return TypedResults.NotFound();

        await cache.EvictByTagAsync("products", ct);
        return TypedResults.NoContent();
    }
}

// ── DTOs and Requests ─────────────────────────────────────────────────────────

public record ProductDto(int Id, string Name, decimal Price, string Category);

public record CreateProductRequest(
    [Required, MinLength(2), MaxLength(100)] string Name,
    [Range(0.01, 1_000_000)] decimal Price,
    [Required] string Category,
    string? Description);

public record UpdateProductRequest(
    [MinLength(2), MaxLength(100)] string? Name,
    [Range(0.01, 1_000_000)] decimal? Price,
    string? Description);

// ── Filters ───────────────────────────────────────────────────────────────────

public class RequestLoggingFilter(ILogger<RequestLoggingFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next)
    {
        var path = ctx.HttpContext.Request.Path;
        logger.LogDebug("→ {Path}", path);
        var sw = Stopwatch.StartNew();
        var result = await next(ctx);
        logger.LogDebug("← {Path} {Elapsed}ms", path, sw.ElapsedMilliseconds);
        return result;
    }
}

public class ValidationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next)
    {
        var model = ctx.Arguments.OfType<T>().FirstOrDefault();
        if (model is null) return TypedResults.BadRequest("Request body required.");

        var validationCtx = new ValidationContext(model);
        var errors = new List<ValidationResult>();

        if (!Validator.TryValidateObject(model, validationCtx, errors, true))
        {
            var dict = errors
                .GroupBy(e => e.MemberNames.FirstOrDefault() ?? string.Empty)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage ?? "Invalid").ToArray());

            return TypedResults.ValidationProblem(dict);
        }

        return await next(ctx);
    }
}
