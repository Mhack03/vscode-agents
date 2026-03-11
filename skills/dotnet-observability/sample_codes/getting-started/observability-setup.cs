// observability-setup.cs — Full Program.cs with Serilog + OpenTelemetry
// Demonstrates: structured logging, OTLP tracing, Prometheus metrics, health checks

using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Templates;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog ─────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, services, config) =>
    config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .WriteTo.Conditional(
            _ => ctx.HostingEnvironment.IsDevelopment(),
            wt => wt.Console(new ExpressionTemplate(
                "[{@t:HH:mm:ss} {@l:u3}] {SourceContext}: {@m}{NewLine}{@x}")))
        .WriteTo.Conditional(
            _ => !ctx.HostingEnvironment.IsDevelopment(),
            wt => wt.Console(new Serilog.Formatting.Compact.CompactJsonFormatter())));

// ─── OpenTelemetry ────────────────────────────────────────────────────────────
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService("MyApi", serviceVersion: "1.0.0")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName.ToLower()
        }))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(opts =>
        {
            opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
            opts.RecordException = true;
        })
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("MyApi.*")
        .AddOtlpExporter(opts =>
            opts.Endpoint = new Uri(
                builder.Configuration["Otel:Endpoint"] ?? "http://localhost:4317")))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("MyApi.*")
        .AddPrometheusExporter());

// ─── Health Checks ────────────────────────────────────────────────────────────
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database", tags: ["db", "ready"])
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost",
        name: "redis", tags: ["cache", "ready"]);

// ─── Correlation ID via HttpContext ───────────────────────────────────────────
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ─── Request logging ──────────────────────────────────────────────────────────
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diag, ctx) =>
    {
        diag.Set("RequestHost", ctx.Request.Host.Value);
        diag.Set("RequestScheme", ctx.Request.Scheme);
        diag.Set("UserAgent", ctx.Request.Headers.UserAgent.ToString());
        if (ctx.User.Identity?.IsAuthenticated == true)
            diag.Set("UserId", ctx.User.FindFirstValue(ClaimTypes.NameIdentifier));
    };
    opts.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

// ─── Correlation middleware ───────────────────────────────────────────────────
app.Use(async (ctx, next) =>
{
    const string header = "X-Correlation-ID";
    var id = ctx.Request.Headers[header].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
    ctx.Response.Headers[header] = id;
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", id))
        await next();
});

// ─── Health Endpoints ─────────────────────────────────────────────────────────
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false,
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = c => c.Tags.Contains("ready"),
});

// ─── Prometheus metrics endpoint ──────────────────────────────────────────────
app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();
