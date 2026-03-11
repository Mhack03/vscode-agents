# Health Checks in ASP.NET Core

---

## Basic Registration

```csharp
// Program.cs
builder.Services
    .AddHealthChecks()
    // Database
    .AddDbContextCheck<AppDbContext>("database",
        failureStatus: HealthStatus.Degraded,
        tags: ["db", "ready"])

    // SQL Server (direct)
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("Sql")!,
        name: "sql-server",
        tags: ["db", "ready"])

    // Redis
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis")!,
        name: "redis",
        tags: ["cache", "ready"])

    // External HTTP dependency
    .AddUrlGroup(
        new Uri("https://api.stripe.com/v1/"),
        name:          "stripe",
        failureStatus: HealthStatus.Degraded,
        tags:          ["external"])

    // Disk space
    .AddDiskStorageHealthCheck(opts =>
        opts.AddDrive("C:\\", minimumFreeMegabytes: 500),
        name: "disk",
        tags: ["infra"]);
```

---

## Kubernetes Probe Endpoints

```csharp
// Liveness — is the app alive? (restart if fails)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,       // No checks — just confirm the HTTP pipeline is alive
    ResponseWriter = WriteHealthResponse,
});

// Readiness — is the app ready for traffic? (remove from LB if fails)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse,
});

// Full report — internal monitoring
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthResponse,
})
.RequireAuthorization("health-reporter");

// JSON response writer
static Task WriteHealthResponse(HttpContext ctx, HealthReport report)
{
    ctx.Response.ContentType = "application/json";
    var result = new
    {
        status  = report.Status.ToString(),
        checks  = report.Entries.Select(e => new
        {
            name        = e.Key,
            status      = e.Value.Status.ToString(),
            description = e.Value.Description,
            duration    = e.Value.Duration.TotalMilliseconds,
            tags        = e.Value.Tags,
        }),
        totalDuration = report.TotalDuration.TotalMilliseconds,
    };
    return ctx.Response.WriteAsJsonAsync(result);
}
```

---

## Custom Health Check

```csharp
// Check an external service your app depends on
public class PaymentGatewayHealthCheck(IPaymentClient client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            var healthy = await client.PingAsync(ct);
            return healthy
                ? HealthCheckResult.Healthy("Payment gateway reachable.")
                : HealthCheckResult.Degraded("Payment gateway responded with an error.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Payment gateway unreachable.",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["endpoint"] = client.BaseUrl,
                });
        }
    }
}

// Register
builder.Services.AddHealthChecks()
    .AddCheck<PaymentGatewayHealthCheck>("payment-gateway",
        failureStatus: HealthStatus.Degraded,
        tags: ["external", "ready"]);
```

---

## HealthChecks UI Dashboard

```bash
dotnet add package AspNetCore.HealthChecks.UI
dotnet add package AspNetCore.HealthChecks.UI.InMemory.Storage
```

```csharp
builder.Services
    .AddHealthChecksUI(opts =>
    {
        opts.SetEvaluationTimeInSeconds(30);
        opts.MaximumHistoryEntriesPerEndpoint(50);
        opts.AddHealthCheckEndpoint("MyApi - Readiness", "/health/ready");
        opts.AddHealthCheckEndpoint("MyApi - Full",      "/health");
    })
    .AddInMemoryStorage();

// Map the dashboard (restrict in prod)
app.MapHealthChecksUI(opts => opts.UIPath = "/health-ui")
   .RequireAuthorization("admin");
```

---

## Kubernetes Probe Config Example

```yaml
# k8s deployment.yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 30
  failureThreshold: 3

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 10
  failureThreshold: 3

startupProbe:
  httpGet:
    path: /health/live
    port: 8080
  failureThreshold: 30
  periodSeconds: 10
```

---

## Learn More

| Topic | Query |
|-------|-------|
| Health checks | `microsoft_docs_search(query="ASP.NET Core health checks IHealthCheck liveness readiness Kubernetes")` |
| HealthChecks UI | `microsoft_docs_search(query="AspNetCore HealthChecks UI dashboard monitoring")` |
