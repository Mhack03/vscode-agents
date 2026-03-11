// mediatr-pipeline.cs — ValidationBehavior + GlobalExceptionHandler
// Demonstrates: MediatR pipeline validation, RFC 7807 error response

using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

// ─── 1. MediatR Validation Pipeline Behavior ──────────────────────────────────
// Runs BEFORE every IRequest handler that has a registered IValidator<TRequest>
public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!validators.Any())
            return await next();

        var ctx = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(ctx, ct)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .ToList();

        if (failures.Count > 0)
        {
            logger.LogDebug("Validation failed for {RequestType}: {Errors}",
                typeof(TRequest).Name,
                string.Join(", ", failures.Select(f => f.ErrorMessage)));

            throw new ValidationException(failures);
        }

        return await next();
    }
}

// ─── 2. Global Exception Handler ─────────────────────────────────────────────
// Catches ValidationException and maps it to RFC 7807 HttpValidationProblemDetails
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx,
        Exception exception,
        CancellationToken ct)
    {
        if (exception is ValidationException valEx)
        {
            var errors = valEx.Errors
                .GroupBy(e => char.ToLower(e.PropertyName[0]) + e.PropertyName[1..])
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            var problem = new HttpValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Type = "https://tools.ietf.org/html/rfc7807",
            };

            problem.Extensions["traceId"] = ctx.TraceIdentifier;

            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            ctx.Response.ContentType = "application/problem+json";
            await ctx.Response.WriteAsJsonAsync(problem, ct);
            return true;
        }

        if (exception is NotFoundException notFoundEx)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found.",
                Detail = notFoundEx.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            };

            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            ctx.Response.ContentType = "application/problem+json";
            await ctx.Response.WriteAsJsonAsync(problem, ct);
            return true;
        }

        // Unhandled exceptions — log and let the built-in handler convert to 500
        logger.LogError(exception, "Unhandled exception for {Method} {Path}",
            ctx.Request.Method, ctx.Request.Path);

        return false;
    }
}

// ─── 3. Registration ──────────────────────────────────────────────────────────
/*
// In Program.cs:
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

app.UseExceptionHandler();
*/

// ─── 4. Sample MediatR command with validation ────────────────────────────────
public record CreateOrderCommand(
    string CustomerId,
    IReadOnlyList<OrderLineRequest> Lines) : IRequest<OrderDto>;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("Order must have at least one line.")
            .Must(l => l.Count <= 50).WithMessage("Maximum 50 lines per order.");
        RuleForEach(x => x.Lines).SetValidator(new OrderLineValidator());
    }
}

public class OrderLineValidator : AbstractValidator<OrderLineRequest>
{
    public OrderLineValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 999);
    }
}

public record OrderLineRequest(int ProductId, int Quantity);
public record OrderDto(Guid Id, string Status);

// Custom exceptions
public class NotFoundException(string message) : Exception(message);
