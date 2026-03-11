---
name: dotnet-validation
description: Input validation for .NET 8/9/10 Web APIs using FluentValidation, MediatR pipeline behaviors, async validators, and consistent Problem Details error shaping.
license: Complete terms in LICENSE.txt
---

# .NET Input Validation Patterns

## When to Use This Skill

- Adding FluentValidation to a .NET Web API
- Setting up a MediatR validation pipeline behavior
- Writing async validators (database uniqueness checks)
- Shaping validation errors as RFC 7807 Problem Details
- Cross-field and conditional validation rules
- Validating route/query parameters in Minimal APIs

---

## FluentValidation Setup

```bash
dotnet add package FluentValidation.AspNetCore
```

```csharp
// Program.cs
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
// Registers all IValidator<T> implementations automatically
```

---

## Defining Validators

```csharp
// Validators/CreateProductValidator.cs
public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    private readonly AppDbContext _context;

    public CreateProductValidator(AppDbContext context)
    {
        _context = context;

        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(100)
            .MustAsync(BeUniqueNameAsync)
            .WithMessage("A product with this name already exists.");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Price must be between $0.01 and $1,000,000.");

        RuleFor(x => x.Category)
            .NotEmpty()
            .IsInEnum()  // For enum types
            .WithMessage("Category is invalid.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);

        // Cross-field rule
        RuleFor(x => x.SalePrice)
            .LessThan(x => x.Price)
            .When(x => x.SalePrice.HasValue)
            .WithMessage("Sale price must be less than the regular price.");
    }

    private async Task<bool> BeUniqueNameAsync(
        string name, CancellationToken ct) =>
        !await _context.Products.AnyAsync(p => p.Name == name, ct);
}
```

---

## Validation Endpoint Filter (Minimal API)

```csharp
// Apply directly on endpoint
orders.MapPost("/", CreateOrder)
      .AddEndpointFilter<ValidationFilter<CreateOrderRequest>>();

// ValidationFilter.cs
public class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next)
    {
        var model = ctx.Arguments.OfType<T>().FirstOrDefault();
        if (model is null) return TypedResults.BadRequest("Body required.");

        var result = await validator.ValidateAsync(model, ctx.HttpContext.RequestAborted);

        return result.IsValid
            ? await next(ctx)
            : TypedResults.ValidationProblem(result.ToDictionary());
    }
}
```

---

## MediatR Pipeline Validation Behavior

```csharp
// Behaviors/ValidationBehavior.cs
public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var ctx = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(ctx, ct))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}

// Register in Program.cs
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddBehavior(typeof(IPipelineBehavior<,>),
                    typeof(ValidationBehavior<,>));
});
```

---

## Consistent Validation Error Responses

```csharp
// GlobalExceptionHandler.cs — handles ValidationException → 400
public class GlobalExceptionHandler(IProblemDetailsService problemDetails)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx,
        Exception exception,
        CancellationToken ct)
    {
        if (exception is ValidationException ve)
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errors = ve.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            await ctx.Response.WriteAsJsonAsync(new HttpValidationProblemDetails(errors)
            {
                Status = 400,
                Title  = "Validation failed",
                Type   = "https://tools.ietf.org/html/rfc7807",
            }, ct);
            return true;
        }
        return false;
    }
}
```

---

## Validation Response Shape

All validation errors return RFC 7807 `application/problem+json`:

```json
{
	"type": "https://tools.ietf.org/html/rfc7807",
	"title": "Validation failed",
	"status": 400,
	"errors": {
		"name": ["A product with this name already exists."],
		"price": ["Price must be between $0.01 and $1,000,000."],
		"salePrice": ["Sale price must be less than the regular price."]
	}
}
```

---

## References

| Topic                                                                | Load When                                                   |
| -------------------------------------------------------------------- | ----------------------------------------------------------- |
| [Advanced FluentValidation](references/advanced-fluentvalidation.md) | Complex rule sets, RuleSets, custom validators, collections |
| [Request Models Design](references/request-models.md)                | Record types, binding from route/query/header, model binder |

## Learn More

| Topic            | Query                                                                                         |
| ---------------- | --------------------------------------------------------------------------------------------- |
| FluentValidation | `microsoft_docs_search(query="FluentValidation ASP.NET Core Minimal API integration .NET 9")` |
| MediatR pipeline | `microsoft_docs_search(query="MediatR pipeline behavior validation IPipelineBehavior")`       |
| Problem Details  | `microsoft_docs_search(query="ASP.NET Core Problem Details RFC 7807 IExceptionHandler")`      |
