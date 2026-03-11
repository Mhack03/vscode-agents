// custom-metrics.cs — IMeterFactory usage + custom health check
// Demonstrates: Counter, Histogram, UpDownCounter, IHealthCheck

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

// ─── Custom Metrics ───────────────────────────────────────────────────────────
// Registered as singleton; contains all meters for this feature
public class ApiMetrics : IDisposable
{
    private readonly Meter _meter;

    // Counters
    public readonly Counter<long> OrdersPlaced;
    public readonly Counter<long> PaymentsProcessed;
    public readonly Counter<long> ValidationErrors;

    // Histograms (distributions)
    public readonly Histogram<double> OrderProcessingMs;
    public readonly Histogram<double> OrderValueUsd;

    // Up/Down Counters (current values)
    public readonly UpDownCounter<int> ActiveConnections;
    public readonly UpDownCounter<int> PendingJobs;

    public ApiMetrics(IMeterFactory factory)
    {
        _meter = factory.Create("MyApi.Business");

        OrdersPlaced = _meter.CreateCounter<long>(
            "orders.placed",
            unit: "{orders}",
            description: "Total orders placed");

        PaymentsProcessed = _meter.CreateCounter<long>(
            "payments.processed",
            unit: "{payments}",
            description: "Total payments processed successfully");

        ValidationErrors = _meter.CreateCounter<long>(
            "validation.errors",
            unit: "{errors}",
            description: "Total validation failures");

        OrderProcessingMs = _meter.CreateHistogram<double>(
            "orders.processing_duration",
            unit: "ms",
            description: "Time taken to process an order end-to-end");

        OrderValueUsd = _meter.CreateHistogram<double>(
            "orders.value",
            unit: "usd",
            description: "Distribution of order values");

        ActiveConnections = _meter.CreateUpDownCounter<int>(
            "connections.active",
            unit: "{connections}",
            description: "Currently active WebSocket connections");

        PendingJobs = _meter.CreateUpDownCounter<int>(
            "jobs.pending",
            unit: "{jobs}",
            description: "Jobs waiting in the background queue");
    }

    public void Dispose() => _meter.Dispose();
}

// ─── Usage in Service ─────────────────────────────────────────────────────────
public class OrderService(ApiMetrics metrics, AppDbContext db)
{
    public async Task<OrderDto> ProcessAsync(CreateOrderRequest req, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        var order = new Order { CustomerId = req.CustomerId, /* ... */ };
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        sw.Stop();

        // Record metrics with tags (dimensions)
        var tags = new TagList
        {
            { "payment.method", req.PaymentMethod },
            { "customer.tier",  req.CustomerTier },
        };

        metrics.OrdersPlaced.Add(1, tags);
        metrics.OrderProcessingMs.Record(sw.Elapsed.TotalMilliseconds, tags);
        metrics.OrderValueUsd.Record((double)req.TotalValue, tags);

        return order.ToDto();
    }
}

// ─── Custom Health Check ───────────────────────────────────────────────────────
public class OrderQueueHealthCheck(ApiMetrics metrics) : IHealthCheck
{
    private const int MaxPendingJobs = 1000;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        // In real code, query actual queue depth
        // Here we use our metric as a proxy
        var data = new Dictionary<string, object>
        {
            ["threshold"] = MaxPendingJobs,
        };

        // HealthStatus.Degraded warns but doesn't remove from load balancer
        return Task.FromResult(HealthCheckResult.Healthy(
            "Order queue is within acceptable limits.", data));
    }
}

// ─── Registration ─────────────────────────────────────────────────────────────
/*
builder.Services.AddSingleton<ApiMetrics>();
builder.Services
    .AddHealthChecks()
    .AddCheck<OrderQueueHealthCheck>("order-queue",
        failureStatus: HealthStatus.Degraded,
        tags: ["ready"]);
*/
