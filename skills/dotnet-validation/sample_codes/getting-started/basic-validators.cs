// basic-validators.cs — FluentValidation setup with Minimal API filter
// Demonstrates: validator registration, async rules, ValidationFilter

using FluentValidation;
using Microsoft.AspNetCore.Http;

// ─── 1. Register validators ──────────────────────────────────────────────────
// In Program.cs:
// builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped);
// builder.Services.AddScoped(typeof(ValidationFilter<>));

// ─── 2. Request model ────────────────────────────────────────────────────────
public record CreateProductRequest(
    string Name,
    string Sku,
    decimal Price,
    int CategoryId,
    string? Description = null);

// ─── 3. Validator with sync and async rules ──────────────────────────────────
public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator(AppDbContext db)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Sku)
            .NotEmpty()
            .Matches(@"^[A-Z0-9\-]{3,20}$")
            .WithMessage("SKU must be 3–20 characters: uppercase letters, digits, and hyphens only.")
            .MustAsync(async (sku, ct) =>
                !await db.Products.AnyAsync(p => p.Sku == sku, ct))
            .WithMessage("SKU is already in use.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.")
            .LessThanOrEqualTo(999_999.99m);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .MustAsync(async (id, ct) =>
                await db.Categories.AnyAsync(c => c.Id == id, ct))
            .WithMessage("Category does not exist.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);
    }
}

// ─── 4. Validation endpoint filter ──────────────────────────────────────────
// Runs validation before the handler; returns RFC 7807 problem on failure
public class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
    where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next)
    {
        var model = ctx.Arguments.OfType<T>().FirstOrDefault();
        if (model is null)
            return await next(ctx);

        var result = await validator.ValidateAsync(model, ctx.HttpContext.RequestAborted);
        if (!result.IsValid)
            return TypedResults.ValidationProblem(result.ToDictionary());

        return await next(ctx);
    }
}

// ─── 5. Endpoint wired up with the filter ────────────────────────────────────
/*
app.MapPost("/api/products",
    async (CreateProductRequest req, IProductService svc, CancellationToken ct) =>
    {
        var product = await svc.CreateAsync(req, ct);
        return TypedResults.Created($"/api/products/{product.Id}", product);
    })
    .AddEndpointFilter<ValidationFilter<CreateProductRequest>>();
*/

// ─── 6. Validation result extension (used in filter) ────────────────────────
public static class ValidationResultExtensions
{
    public static Dictionary<string, string[]> ToDictionary(
        this FluentValidation.Results.ValidationResult result) =>
        result.Errors
            .GroupBy(e => char.ToLower(e.PropertyName[0]) + e.PropertyName[1..])
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
}
