// ============================================================
// Async / Await Best Practices in .NET
// ============================================================

// ── 1. CancellationToken Propagation ─────────────────────────

public class OrderService(IOrderRepository repo)
{
    // ✅ Always accept and forward CancellationToken
    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default) =>
        await repo.GetAllAsync(ct);

    // ✅ Pass to all async calls in a chain
    public async Task<OrderSummary> GetSummaryAsync(Guid id, CancellationToken ct = default)
    {
        var order = await repo.GetByIdAsync(id, ct);
        var customer = await _customerRepo.GetByIdAsync(order!.CustomerId, ct);
        var invoice = await _invoiceSvc.GenerateAsync(order, ct);
        return new OrderSummary(order, customer!, invoice);
    }
}

// ── 2. ConfigureAwait(false) in Library/Infrastructure Code ───

public class FileStorageService
{
    // ✅ Use ConfigureAwait(false) in non-UI library code — avoids capturing sync context
    public async Task<string> ReadAsync(string path, CancellationToken ct = default)
    {
        using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
    }

    // Note: In ASP.NET Core there is no SynchronizationContext, so ConfigureAwait(false)
    // has no functional effect — but is still a good habit in shared library code.
}

// ── 3. Return Task Directly (Eliding async/await) ─────────────

public class CacheService(IOrderRepository repo)
{
    // ✅ Elide async/await when just forwarding — no using/try/finally wrapping needed
    public Task<Order?> GetOrderAsync(Guid id, CancellationToken ct = default) =>
        repo.GetByIdAsync(id, ct);

    // ❌ Unnecessary state machine overhead
    public async Task<Order?> GetOrderBad(Guid id, CancellationToken ct = default) =>
        await repo.GetByIdAsync(id, ct);

    // ✅ MUST use async/await if inside using or try/catch
    public async Task<Order?> GetOrderWithLogging(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await repo.GetByIdAsync(id, ct);
        }
        catch (Exception ex)
        {
            // If elided, this catch would run after the Task completes — potentially
            // swallowing the exception. Always use async/await with try/catch.
            Console.Error.WriteLine(ex.Message);
            return null;
        }
    }
}

// ── 4. ValueTask for Hot Paths ────────────────────────────────

public class ProductCache
{
    private readonly Dictionary<Guid, Product> _cache = [];

    // ✅ ValueTask avoids Task allocation when the result is already cached (synchronous path)
    public ValueTask<Product?> GetProductAsync(Guid id, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(id, out var cached))
            return ValueTask.FromResult<Product?>(cached);  // Synchronous — no alloc

        return FetchAndCacheAsync(id, ct);  // Async path — allocates Task once
    }

    private async ValueTask<Product?> FetchAndCacheAsync(Guid id, CancellationToken ct)
    {
        var product = await FetchFromDbAsync(id, ct);
        if (product is not null) _cache[id] = product;
        return product;
    }

    private Task<Product?> FetchFromDbAsync(Guid id, CancellationToken ct) =>
        Task.FromResult<Product?>(null); // Stub
}

// ── 5. Parallel Async Work ────────────────────────────────────

public class DashboardService(
    IOrderRepository orderRepo,
    ICustomerRepository customerRepo,
    IProductRepository productRepo)
{
    // ✅ Parallel — all three queries run concurrently
    public async Task<DashboardData> LoadAsync(CancellationToken ct = default)
    {
        var ordersTask = orderRepo.GetRecentAsync(10, ct);
        var customersTask = customerRepo.GetActiveCountAsync(ct);
        var productsTask = productRepo.GetLowStockAsync(ct);

        await Task.WhenAll(ordersTask, customersTask, productsTask);

        return new DashboardData(
            Orders: await ordersTask,
            ActiveCustomers: await customersTask,
            LowStockProducts: await productsTask);
    }

    // ✅ Fan-out with error handling — don't let one failure cancel others
    public async Task<ProcessingResult[]> ProcessBatchAsync(
        IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var tasks = ids.Select(id => ProcessSingleAsync(id, ct));
        return await Task.WhenAll(tasks);  // All complete (or fail) before returning
    }

    // ❌ Sequential — slower, waits for each before starting the next
    public async Task<DashboardData> LoadSequential(CancellationToken ct = default)
    {
        var orders = await orderRepo.GetRecentAsync(10, ct);
        var customers = await customerRepo.GetActiveCountAsync(ct);
        var products = await productRepo.GetLowStockAsync(ct);
        return new DashboardData(orders, customers, products);
    }

    private Task<ProcessingResult> ProcessSingleAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(new ProcessingResult(id, true)); // Stub
}

// ── 6. IAsyncEnumerable<T> — Streaming Results ────────────────

public class ReportService(AppDbContext db)
{
    // ✅ Stream large result sets row-by-row — doesn't buffer all rows in memory
    public async IAsyncEnumerable<ReportRow> StreamReportAsync(
        DateRange range,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var order in db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= range.Start && o.CreatedAt <= range.End)
            .AsAsyncEnumerable()
            .WithCancellation(ct))
        {
            yield return new ReportRow(order.Id, order.CreatedAt, order.Total);
        }
    }
}

// Minimal API streaming endpoint
// app.MapGet("/reports/stream", async (ReportService svc, CancellationToken ct) =>
//     svc.StreamReportAsync(DateRange.LastMonth, ct));

// ── 7. Deadlock Avoidance ─────────────────────────────────────

public class LegacyBridge
{
    private readonly IOrderService _svc;

    // ❌ Deadlocks in classic ASP.NET (SynchronizationContext causes deadlock)
    // public Order GetOrderDeadlock(Guid id) => _svc.GetByIdAsync(id).Result;

    // ✅ Safe blocking in genuinely sync-only contexts (console, tests)
    // Use sparingly — prefer async all the way down
    public Order? GetOrderSync(Guid id) =>
        _svc.GetByIdAsync(id).GetAwaiter().GetResult(); // Better than .Result for stack traces

    // ✅ Best — propagate async all the way up
    public async Task<Order?> GetOrderAsync(Guid id, CancellationToken ct = default) =>
        await _svc.GetByIdAsync(id, ct);
}

// ── 8. Timeout Pattern ────────────────────────────────────────

public class ExternalApiClient(HttpClient http)
{
    public async Task<string?> FetchWithTimeoutAsync(
        string url, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(10));  // Hard timeout

        try
        {
            var response = await http.GetAsync(url, cts.Token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Our 10s timeout fired, not the caller's token
            return null;
        }
    }
}

// ── Placeholder Stubs ─────────────────────────────────────────
record Order(Guid Id, Guid CustomerId, DateTime CreatedAt, decimal Total);
record Customer(Guid Id, string Name);
record Product(Guid Id, string Name);
record OrderSummary(Order Order, Customer Customer, object Invoice);
record DashboardData(IReadOnlyList<Order> Orders, int ActiveCustomers, IReadOnlyList<Product> LowStockProducts);
record ProcessingResult(Guid Id, bool Success);
record ReportRow(Guid OrderId, DateTime Date, decimal Total);
record DateRange(DateTime Start, DateTime End)
{
    public static DateRange LastMonth =>
        new(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
}

interface IOrderService { Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default); }
interface IOrderRepository
{
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetRecentAsync(int count, CancellationToken ct = default);
}
interface ICustomerRepository { Task<int> GetActiveCountAsync(CancellationToken ct = default); }
interface IProductRepository { Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken ct = default); }
class AppDbContext { public IQueryable<Order> Orders => null!; }
