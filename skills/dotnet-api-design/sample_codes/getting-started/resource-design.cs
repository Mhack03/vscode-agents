// resource-design.cs — RESTful routes, status codes, and versioned group setup
// Demonstrates: route naming, TypedResults, minimal versioned groups

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ─── Route Groups (Versioned) ─────────────────────────────────────────────────
var v1 = app.MapGroup("/api/v1")
            .WithOpenApi()
            .RequireAuthorization();

var products = v1.MapGroup("/products").WithTags("Products");

// ─── GET /api/v1/products ─────────────────────────────────────────────────────
// 200 OK — returns a page of products
products.MapGet("", async (
    [AsParameters] GetProductsQuery query,
    IProductService svc,
    CancellationToken ct) =>
{
    var page = await svc.GetPageAsync(query, ct);
    return TypedResults.Ok(page);
})
.WithSummary("List products")
.WithDescription("Returns a paginated list of products. Supports filtering and sorting.");

// ─── GET /api/v1/products/{id} ────────────────────────────────────────────────
// 200 OK | 404 Not Found
products.MapGet("{id:int}", async (int id, IProductService svc, CancellationToken ct) =>
{
    var product = await svc.GetByIdAsync(id, ct);
    return product is null
        ? TypedResults.NotFound()
        : TypedResults.Ok(product);
})
.WithSummary("Get a product by ID")
.Produces<ProductDto>(200)
.ProducesProblem(404);

// ─── POST /api/v1/products ────────────────────────────────────────────────────
// 201 Created | 400 Bad Request | 409 Conflict
products.MapPost("", async (
    CreateProductRequest req,
    IProductService svc,
    CancellationToken ct) =>
{
    var product = await svc.CreateAsync(req, ct);
    return TypedResults.Created($"/api/v1/products/{product.Id}", product);
})
.WithSummary("Create a product")
.Produces<ProductDto>(201)
.ProducesValidationProblem()
.ProducesProblem(409)
.AddEndpointFilter<ValidationFilter<CreateProductRequest>>();

// ─── PUT /api/v1/products/{id} ────────────────────────────────────────────────
// 200 OK | 400 Bad Request | 404 Not Found
products.MapPut("{id:int}", async (
    int id,
    UpdateProductRequest req,
    IProductService svc,
    CancellationToken ct) =>
{
    var product = await svc.UpdateAsync(id, req, ct);
    return product is null
        ? TypedResults.NotFound()
        : TypedResults.Ok(product);
})
.WithSummary("Update a product")
.AddEndpointFilter<ValidationFilter<UpdateProductRequest>>();

// ─── DELETE /api/v1/products/{id} ─────────────────────────────────────────────
// 204 No Content | 404 Not Found
products.MapDelete("{id:int}", async (
    int id,
    IProductService svc,
    CancellationToken ct) =>
{
    var deleted = await svc.DeleteAsync(id, ct);
    return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
})
.WithSummary("Delete a product")
.RequireAuthorization("admin");

// ─── Request / Response Types ─────────────────────────────────────────────────
public record GetProductsQuery(
    [FromQuery] string? SearchTerm = null,
    [FromQuery] string? Category = null,
    [FromQuery] int Page = 1,
    [FromQuery] int Size = 25,
    [FromQuery] string Sort = "name_asc");

public record CreateProductRequest(
    string Name,
    string Sku,
    decimal Price,
    int CategoryId);

public record UpdateProductRequest(
    string? Name,
    decimal? Price,
    string? Description);

public record ProductDto(
    int Id,
    string Name,
    string Sku,
    decimal Price,
    string Category);

app.Run();
