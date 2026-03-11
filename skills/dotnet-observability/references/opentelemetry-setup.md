# OpenTelemetry Setup & Tracing Configuration

---

## Full OTEL Configuration

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName:        builder.Configuration["Otel:ServiceName"] ?? "MyApi",
            serviceVersion:     Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion,
            serviceInstanceId:  Environment.MachineName)
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName.ToLower(),
        }))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(opts =>
        {
            opts.Filter = ctx =>
                !ctx.Request.Path.StartsWithSegments("/health") &&
                !ctx.Request.Path.StartsWithSegments("/metrics");
            opts.EnrichWithHttpRequest  = (activity, req)  => activity.SetTag("http.request.body.size",  req.ContentLength);
            opts.EnrichWithHttpResponse = (activity, resp) => activity.SetTag("http.response.body.size", resp.ContentLength);
            opts.RecordException = true;
        })
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(opts =>
        {
            opts.SetDbStatementForText = true;  // Log SQL — disable in prod with sensitive data
        })
        .AddSource("MyApi.*")                     // Custom ActivitySources
        .AddOtlpExporter(opts =>
        {
            opts.Endpoint = new Uri(builder.Configuration["Otel:Endpoint"] ?? "http://localhost:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("MyApi.*")                      // Custom meters
        .AddPrometheusExporter()
        .AddOtlpExporter())
    .WithLogging(logging => logging
        .AddOtlpExporter());

// Expose Prometheus scrape endpoint
app.MapPrometheusScrapingEndpoint("/metrics")
   .RequireAuthorization("metrics-scraper");
```

---

## Custom Spans with ActivitySource

```csharp
// Register a source per feature area
public static class Telemetry
{
    public static readonly ActivitySource OrdersSource  = new("MyApi.Orders",  "1.0.0");
    public static readonly ActivitySource PaymentSource = new("MyApi.Payment", "1.0.0");
}

// Usage in service
public class OrderService
{
    public async Task<OrderDto> ProcessOrderAsync(CreateOrderRequest req, CancellationToken ct)
    {
        using var activity = Telemetry.OrdersSource.StartActivity(
            "ProcessOrder", ActivityKind.Internal);

        activity?.SetTag("order.customer_id", req.CustomerId);
        activity?.SetTag("order.item_count",  req.Lines.Count);

        try
        {
            var order = await CreateOrderInternal(req, ct);
            activity?.SetTag("order.id", order.Id);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return order.ToDto();
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

---

## Baggage Propagation (Cross-Service)

```csharp
// Set baggage at entry point (e.g., API gateway)
Activity.Current?.SetBaggage("tenant.id", tenantId);
Activity.Current?.SetBaggage("user.id",   userId);

// Read baggage downstream (e.g., in a downstream service or consumer)
var tenantId = Activity.Current?.GetBaggageItem("tenant.id");
var userId   = Activity.Current?.GetBaggageItem("user.id");
```

---

## Sampling Strategies

```csharp
.WithTracing(tracing => tracing
    // Always sample — best for development
    // .SetSampler(new AlwaysOnSampler())

    // Sample 10% in production
    .SetSampler(new TraceIdRatioBasedSampler(0.1))

    // Always sample errors and slow requests
    .SetSampler(new CompositeTraceSampler([
        new AlwaysOnSampler(),        // Can replace with custom rule
        new TraceIdRatioBasedSampler(0.05),
    ]))
)
```

---

## Grafana / Jaeger Endpoints

```yaml
# docker-compose.yml — local observability stack
services:
  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686"   # UI
      - "4317:4317"     # OTLP gRPC
      - "4318:4318"     # OTLP HTTP

  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
```

```yaml
# prometheus.yml
scrape_configs:
  - job_name: myapi
    static_configs:
      - targets: ["host.docker.internal:5000"]
    metrics_path: /metrics
```

---

## Correlation ID Middleware

```csharp
public class CorrelationIdMiddleware(RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    private const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext ctx)
    {
        var correlationId = ctx.Request.Headers[HeaderName].FirstOrDefault()
                            ?? Guid.NewGuid().ToString("N");

        ctx.Items["CorrelationId"] = correlationId;
        ctx.Response.Headers[HeaderName] = correlationId;

        // Make available to Serilog
        using (LogContext.PushProperty("CorrelationId", correlationId))
        // Make available to OTEL
        using (Activity.Current?.AddTag("correlation.id", correlationId) is not null
                ? new DummyDisposable() : new DummyDisposable())
        {
            await next(ctx);
        }
    }

    private sealed class DummyDisposable : IDisposable { public void Dispose() { } }
}
```

---

## Learn More

| Topic | Query |
|-------|-------|
| OpenTelemetry .NET | `microsoft_docs_search(query="OpenTelemetry .NET tracing metrics OTLP exporter configuration")` |
| ActivitySource | `microsoft_docs_search(query=".NET ActivitySource custom spans distributed tracing")` |
| Prometheus .NET | `microsoft_docs_search(query="ASP.NET Core Prometheus metrics scraping OpenTelemetry")` |
