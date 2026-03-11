// ============================================================
// Result Pattern — Functional Error Handling Without Exceptions
// ============================================================
// Use exceptions for truly exceptional conditions (network outage,
// programming errors). Use Result<T> for expected failure paths
// (validation failures, not found, business rule violations).
// ============================================================

// ── 1. Core Result Type ───────────────────────────────────────

public readonly record struct Result<T>
{
    public T? Value { get; }
    public string? Error { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private Result(T value) { Value = value; IsSuccess = true; }
    private Result(string error) { Error = error; IsSuccess = false; }

    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(string error) => new(error);

    // Map — transform the value if success (like LINQ Select)
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper) =>
        IsSuccess ? Result<TOut>.Ok(mapper(Value!)) : Result<TOut>.Fail(Error!);

    // Bind — chain operations that each return Result<T>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> next) =>
        IsSuccess ? next(Value!) : Result<TOut>.Fail(Error!);

    // Match — consume the result (like a switch expression)
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<string, TOut> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error!);

    // Implicit conversion from T for ergonomic returns
    public static implicit operator Result<T>(T value) => Ok(value);
}

// ── Non-generic Result (for operations with no return value) ──

public readonly record struct Result
{
    public string? Error { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private Result(bool success, string? error) { IsSuccess = success; Error = error; }

    public static Result Ok() => new(true, null);
    public static Result Fail(string error) => new(false, error);

    public static implicit operator Result(string error) => Fail(error);
}

// ── 2. Error Types (Discriminated Union Style) ────────────────

public abstract record AppError(string Message)
{
    public record NotFound(string Resource, Guid Id)
        : AppError($"{Resource} with id '{Id}' was not found.");

    public record Validation(string Field, string Message)
        : AppError($"Validation failed for '{Field}': {Message}");

    public record Conflict(string Message) : AppError(Message);

    public record Unauthorized(string Message = "Unauthorized.") : AppError(Message);
}

// Result with typed errors
public readonly record struct Result<T, TError>
{
    public T? Value { get; }
    public TError? Error { get; }
    public bool IsSuccess { get; }

    private Result(T value) { Value = value; IsSuccess = true; }
    private Result(TError error) { Error = error; IsSuccess = false; }

    public static Result<T, TError> Ok(T value) => new(value);
    public static Result<T, TError> Fail(TError error) => new(error);
}

// ── 3. Service Layer Usage ────────────────────────────────────

public class OrderService(
    IOrderRepository repo,
    ICustomerRepository customerRepo,
    ILogger<OrderService> logger)
{
    public async Task<Result<OrderDto>> CreateOrderAsync(
        CreateOrderRequest req, CancellationToken ct = default)
    {
        // Validation
        if (!req.Lines.Any())
            return Result<OrderDto>.Fail("Order must have at least one line.");

        if (req.Lines.Any(l => l.Quantity <= 0))
            return Result<OrderDto>.Fail("All line quantities must be positive.");

        // Business rule check
        var customer = await customerRepo.GetByIdAsync(req.CustomerId, ct);
        if (customer is null)
            return Result<OrderDto>.Fail($"Customer {req.CustomerId} not found.");

        if (!customer.IsActive)
            return Result<OrderDto>.Fail("Cannot create orders for inactive customers.");

        // Create
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = req.CustomerId,
            Lines = req.Lines.Select(l => new OrderLine(l.ProductId, l.Quantity, 0)).ToList(),
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending
        };

        repo.Add(order);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Order {Id} created for customer {CustomerId}",
            order.Id, customer.Id);

        return Result<OrderDto>.Ok(new OrderDto(order.Id, order.Status, order.CreatedAt));
    }

    public async Task<Result> CancelOrderAsync(Guid id, CancellationToken ct = default)
    {
        var order = await repo.GetByIdAsync(id, ct);

        if (order is null)
            return Result.Fail($"Order {id} not found.");

        if (order.Status == OrderStatus.Shipped)
            return Result.Fail("Cannot cancel an order that has already been shipped.");

        if (order.Status == OrderStatus.Cancelled)
            return Result.Fail("Order is already cancelled.");

        order.Status = OrderStatus.Cancelled;
        await repo.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

// ── 4. Minimal API Endpoint — Mapping Result to IResult ───────

static class ResultExtensions
{
    // Result<T> → IResult for minimal APIs
    public static IResult ToHttpResult<T>(this Result<T> result, string? location = null) =>
        result.IsSuccess
            ? location is not null
                ? Results.Created(location, result.Value)
                : Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 400);

    // Result → IResult
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess ? Results.NoContent() : Results.Problem(result.Error, statusCode: 400);
}

// In endpoint:
// static async Task<IResult> Create(CreateOrderRequest req, OrderService svc, CancellationToken ct)
// {
//     var result = await svc.CreateOrderAsync(req, ct);
//     return result.ToHttpResult($"/api/orders/{result.Value?.Id}");
// }

// ── 5. Chaining Results (Railway-Oriented Programming) ────────

public class PipelineExample
{
    public Result<InvoiceDto> ProcessOrder(CreateOrderRequest req)
    {
        return ValidateRequest(req)
            .Bind(CheckInventory)
            .Bind(CalculatePricing)
            .Map(priced => new InvoiceDto(Guid.NewGuid(), priced.Total));
    }

    private Result<CreateOrderRequest> ValidateRequest(CreateOrderRequest req) =>
        req.Lines.Any()
            ? Result<CreateOrderRequest>.Ok(req)
            : Result<CreateOrderRequest>.Fail("No lines provided.");

    private Result<CreateOrderRequest> CheckInventory(CreateOrderRequest req) =>
        // Would query inventory service
        Result<CreateOrderRequest>.Ok(req);

    private Result<PricedOrder> CalculatePricing(CreateOrderRequest req) =>
        Result<PricedOrder>.Ok(new PricedOrder(req, 99.99m));
}

// ── Stubs ─────────────────────────────────────────────────────
enum OrderStatus { Pending, Processing, Shipped, Completed, Cancelled }
record Order
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; init; }
    public List<OrderLine> Lines { get; init; } = [];
}
record OrderLine(Guid ProductId, int Quantity, decimal Price);
record OrderDto(Guid Id, OrderStatus Status, DateTime CreatedAt);
record InvoiceDto(Guid Id, decimal Total);
record PricedOrder(CreateOrderRequest Request, decimal Total);
record Customer { public Guid Id { get; init; } public bool IsActive { get; init; } }
record CreateOrderRequest(Guid CustomerId, List<OrderLineRequest> Lines);
record OrderLineRequest(Guid ProductId, int Quantity);

interface IOrderRepository
{
    void Add(Order o);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken ct);
}
interface ICustomerRepository { Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct); }
