// ============================================================
// Minimal Web API — ASP.NET Core 8/9/10 Getting Started
// ============================================================
// dotnet new webapi -n MyApp.Api --use-minimal-apis
// ============================================================

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Services ────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();          // .NET 9+ built-in OpenAPI

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=app.db"));

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p =>
        p.WithOrigins("http://localhost:5173", "https://localhost:5173") // Vite
         .AllowAnyMethod()
         .AllowAnyHeader()));

// ── Build ────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json
}

app.UseHttpsRedirection();
app.UseCors();

// ── Endpoints ────────────────────────────────────────────────
app.MapProductEndpoints();

app.Run();

// ── Endpoint Definitions ─────────────────────────────────────
static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
                       .WithTags("Products");

        group.MapGet("/", GetAll);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);

        return app;
    }

    static async Task<Ok<IReadOnlyList<ProductDto>>> GetAll(
        IProductService svc, CancellationToken ct) =>
        TypedResults.Ok(await svc.GetAllAsync(ct));

    static async Task<Results<Ok<ProductDto>, NotFound>> GetById(
        Guid id, IProductService svc, CancellationToken ct)
    {
        var product = await svc.GetByIdAsync(id, ct);
        return product is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(product);
    }

    static async Task<Results<Created<ProductDto>, BadRequest<string>>> Create(
        CreateProductRequest req, IProductService svc, CancellationToken ct)
    {
        var result = await svc.CreateAsync(req, ct);
        return result.IsSuccess
            ? TypedResults.Created($"/api/products/{result.Value!.Id}", result.Value)
            : TypedResults.BadRequest(result.Error);
    }

    static async Task<Results<NoContent, NotFound>> Update(
        Guid id, UpdateProductRequest req, IProductService svc, CancellationToken ct) =>
        await svc.UpdateAsync(id, req, ct)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();

    static async Task<Results<NoContent, NotFound>> Delete(
        Guid id, IProductService svc, CancellationToken ct) =>
        await svc.DeleteAsync(id, ct)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
}

// ── Domain ───────────────────────────────────────────────────
public class Product
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime Created { get; init; } = DateTime.UtcNow;
}

// ── DTOs (Records) ────────────────────────────────────────────
public record ProductDto(Guid Id, string Name, string Sku, decimal Price);
public record CreateProductRequest(string Name, string Sku, decimal Price);
public record UpdateProductRequest(string Name, decimal Price);

// ── Interfaces ────────────────────────────────────────────────
public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    void Add(Product product);
    void Remove(Product product);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default);
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<ProductDto>> CreateAsync(CreateProductRequest req, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guid id, UpdateProductRequest req, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

// ── Infrastructure ────────────────────────────────────────────
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.Sku).HasMaxLength(50).IsRequired();
            e.Property(p => p.Price).HasColumnType("decimal(18,2)");
        });
    }
}

public class ProductRepository(AppDbContext db) : IProductRepository
{
    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default) =>
        await db.Products.AsNoTracking().ToListAsync(ct);

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public void Add(Product product) => db.Products.Add(product);
    public void Remove(Product product) => db.Products.Remove(product);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

// ── Application Service ───────────────────────────────────────
public class ProductService(IProductRepository repo) : IProductService
{
    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default)
    {
        var products = await repo.GetAllAsync(ct);
        return products.Select(ToDto).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var product = await repo.GetByIdAsync(id, ct);
        return product is null ? null : ToDto(product);
    }

    public async Task<Result<ProductDto>> CreateAsync(
        CreateProductRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Result<ProductDto>.Fail("Name is required.");
        if (req.Price <= 0)
            return Result<ProductDto>.Fail("Price must be positive.");

        var product = new Product { Name = req.Name, Sku = req.Sku, Price = req.Price };
        repo.Add(product);
        await repo.SaveChangesAsync(ct);
        return Result<ProductDto>.Ok(ToDto(product));
    }

    public async Task<bool> UpdateAsync(
        Guid id, UpdateProductRequest req, CancellationToken ct = default)
    {
        var product = await repo.GetByIdAsync(id, ct);
        if (product is null) return false;

        product.Name = req.Name;
        product.Price = req.Price;
        await repo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await repo.GetByIdAsync(id, ct);
        if (product is null) return false;

        repo.Remove(product);
        await repo.SaveChangesAsync(ct);
        return true;
    }

    private static ProductDto ToDto(Product p) =>
        new(p.Id, p.Name, p.Sku, p.Price);
}

// ── Result type (also see result-pattern.cs) ─────────────────
public readonly record struct Result<T>
{
    public T? Value { get; }
    public string? Error { get; }
    public bool IsSuccess { get; }

    private Result(T value) { Value = value; IsSuccess = true; }
    private Result(string error) { Error = error; IsSuccess = false; }

    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(string error) => new(error);
}
