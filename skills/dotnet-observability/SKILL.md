---
name: dotnet-observability
description: Structured logging (Serilog), OpenTelemetry tracing and metrics, health checks, distributed correlation, and diagnostics for .NET 8/9/10 production APIs.
license: Complete terms in LICENSE.txt
---

# .NET Observability & Logging

## When to Use This Skill

- Setting up Serilog with structured logging and sinks
- Configuring OpenTelemetry traces, metrics, and logs
- Adding health checks with detailed responses
- Implementing correlation IDs across requests and services
- Profiling with dotnet-trace and dotnet-counters
- Integrating with Azure Monitor / Application Insights / Grafana

---

## Serilog Setup

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.ApplicationInsights  # Optional
```

```csharp
// Program.cs — configure before builder.Build()
builder.Host.UseSerilog((ctx, services, config) =>
    config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .WriteTo.Console(new ExpressionTemplate(
            "[{@t:HH:mm:ss} {@l:u3}] {@m} {SourceContext}\n{@x}"))
        .WriteTo.File(
            path:             "logs/api-.json",
            formatter:        new CompactJsonFormatter(),
            rollingInterval:  RollingInterval.Day,
            retainedFileCountLimit: 30)
);

// appsettings.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
        "System.Net.Http.HttpClient": "Warning"
      }
    }
  }
}
```

---

## Request Logging Middleware

```csharp
// Logs every request with status, elapsed time, and user context
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0}ms)";
    options.EnrichDiagnosticContext = (diag, ctx) =>
    {
        diag.Set("UserAgent",  ctx.Request.Headers.UserAgent);
        diag.Set("ClientIp",   ctx.Connection.RemoteIpAddress?.ToString());
        diag.Set("UserId",     ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        diag.Set("RequestId",  ctx.TraceIdentifier);
    };
});
```

---

## OpenTelemetry — Traces + Metrics + Logs

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore
dotnet add package OpenTelemetry.Exporter.Otlp            # For Grafana/Jaeger
dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore  # For App Insights
```

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService("MyApi", serviceVersion: "1.0.0")
        .AddAttributes([new("environment", builder.Environment.EnvironmentName)]))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(o => o.RecordException = true)
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(o => o.SetDbStatementForText = true)
        .AddOtlpExporter())  // Sends to Grafana/Jaeger/Tempo
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()  // GC, thread pool, memory
        .AddPrometheusExporter())     // /metrics endpoint
    .WithLogging(logging => logging
        .AddOtlpExporter());

// Expose Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint("/metrics")
   .RequireAuthorization("InternalOnly");
```

---

## Custom Metrics

```csharp
// Metrics/ApiMetrics.cs
public class ApiMetrics
{
    private readonly Counter<long>     _ordersCreated;
    private readonly Histogram<double> _orderProcessingTime;
    private readonly UpDownCounter<long> _activeCheckouts;

    public ApiMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("MyApi");
        _ordersCreated       = meter.CreateCounter<long>("orders.created.total");
        _orderProcessingTime = meter.CreateHistogram<double>(
            "orders.processing.duration.ms", "ms");
        _activeCheckouts     = meter.CreateUpDownCounter<long>("checkouts.active");
    }

    public void RecordOrderCreated(string region) =>
        _ordersCreated.Add(1, new TagList { { "region", region } });

    public void RecordProcessingDuration(double ms) =>
        _orderProcessingTime.Record(ms);
}

builder.Services.AddSingleton<ApiMetrics>();
```

---

## Health Checks

```bash
dotnet add package AspNetCore.HealthChecks.UI
dotnet add package AspNetCore.HealthChecks.SqlServer
dotnet add package AspNetCore.HealthChecks.Redis
```

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database",        tags: ["db", "critical"])
    .AddSqlServer(connStr,           name: "sql",       tags: ["db"])
    .AddRedis(redisConn,             name: "redis",     tags: ["cache"])
    .AddUrlGroup(new Uri("https://external-api.com/health"),
                 name: "external-api",                 tags: ["deps"])
    .AddCheck<DiskSpaceHealthCheck>("disk-space",       tags: ["infra"]);

// Endpoints
app.MapHealthChecks("/healthz",  // Simple liveness (k8s liveness probe)
    new HealthCheckOptions { Predicate = _ => false });  // Always 200 if app running

app.MapHealthChecks("/healthz/ready",  // Readiness — all checks
    new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        ResultStatusCodes =
        {
            [HealthStatus.Healthy]   = StatusCodes.Status200OK,
            [HealthStatus.Degraded]  = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
        }
    });
```

---

## Correlation ID Middleware

```csharp
// Propagates X-Correlation-ID header across incoming → outgoing requests
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string Header = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext ctx)
    {
        var correlationId = ctx.Request.Headers[Header].FirstOrDefault()
            ?? Activity.Current?.Id
            ?? ctx.TraceIdentifier;

        ctx.Response.Headers[Header] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (var activity = new ActivitySource("MyApi").StartActivity("Request"))
        {
            activity?.SetTag("correlation_id", correlationId);
            await next(ctx);
        }
    }
}
```

---

## Structured Logging Best Practices

```csharp
// ✅ Structured — queryable in log aggregators
_logger.LogInformation(
    "Order {OrderId} created for customer {CustomerId} with {ItemCount} items",
    order.Id, order.CustomerId, order.Lines.Count);

// ❌ String interpolation — loses structure, can't query/filter
_logger.LogInformation($"Order {order.Id} created for customer {order.CustomerId}");

// ✅ Use LoggerMessage.Define for high-frequency paths (zero allocation)
private static readonly Action<ILogger, int, string, Exception?> _orderCreated =
    LoggerMessage.Define<int, string>(
        LogLevel.Information,
        new EventId(1001, "OrderCreated"),
        "Order {OrderId} created by {UserId}");

_orderCreated(_logger, order.Id, userId, null);
```

---

## References

| Topic                                                        | Load When                                                  |
| ------------------------------------------------------------ | ---------------------------------------------------------- |
| [Serilog Sinks & Enrichers](references/serilog-sinks.md)     | Seq, Application Insights, Elasticsearch, Datadog sinks    |
| [OpenTelemetry Deep Dive](references/opentelemetry-setup.md) | Grafana Tempo, Jaeger, OTLP collector, distributed tracing |
| [Health Check Patterns](references/health-checks.md)         | Custom health checks, k8s probes, health check UI          |

## Learn More

| Topic              | Query                                                                                                           |
| ------------------ | --------------------------------------------------------------------------------------------------------------- |
| Serilog            | `microsoft_docs_search(query="Serilog ASP.NET Core structured logging enrichers sinks")`                        |
| OpenTelemetry .NET | `microsoft_docs_fetch(url="https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel")` |
| Health checks      | `microsoft_docs_search(query="ASP.NET Core health checks IHealthCheck readiness liveness Kubernetes")`          |
