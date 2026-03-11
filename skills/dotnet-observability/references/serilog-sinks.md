# Serilog Sinks & Enrichers

---

## Common Sinks

```bash
# Console (dev)
dotnet add package Serilog.Sinks.Console

# File (rolling)
dotnet add package Serilog.Sinks.File

# Seq (self-hosted log server — great for local dev)
dotnet add package Serilog.Sinks.Seq

# Application Insights (Azure)
dotnet add package Serilog.Sinks.ApplicationInsights

# Elasticsearch / OpenSearch
dotnet add package Serilog.Sinks.Elasticsearch

# Datadog
dotnet add package Serilog.Sinks.Datadog.Logs

# OpenTelemetry sink (recommended for cloud-native)
dotnet add package Serilog.Sinks.OpenTelemetry
```

---

## Common Enrichers

```bash
dotnet add package Serilog.Enrichers.Environment       # MachineName, EnvironmentName
dotnet add package Serilog.Enrichers.Thread            # ThreadId
dotnet add package Serilog.Enrichers.Process           # ProcessId
dotnet add package Serilog.Enrichers.Span              # TraceId, SpanId from Activity
dotnet add package Serilog.Enrichers.CorrelationId     # CorrelationId from header
```

---

## Full Production Configuration

```csharp
builder.Host.UseSerilog((ctx, services, config) =>
    config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithSpan()               // Adds TraceId, SpanId (requires Activity)
        .Enrich.WithCorrelationId()      // Adds X-Correlation-ID value
        .Enrich.WithProperty("Application", "MyApi")
        .Enrich.WithProperty("Version",
            Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion ?? "unknown")

        // Dev: human-readable console
        .WriteTo.Conditional(
            e => ctx.HostingEnvironment.IsDevelopment(),
            wt => wt.Console(new ExpressionTemplate(
                "[{@t:HH:mm:ss} {@l:u3}] {SourceContext}: {@m}{NewLine}{@x}")))

        // Prod: JSON to stdout (picked up by container log aggregator)
        .WriteTo.Conditional(
            e => !ctx.HostingEnvironment.IsDevelopment(),
            wt => wt.Console(new CompactJsonFormatter()))

        // Always: Seq in dev for search
        .WriteTo.Conditional(
            e => ctx.HostingEnvironment.IsDevelopment(),
            wt => wt.Seq("http://localhost:5341"))

        // Always: OpenTelemetry for traces correlation
        .WriteTo.OpenTelemetry(otel =>
        {
            otel.Endpoint = ctx.Configuration["Otel:Endpoint"] ?? "http://localhost:4317";
            otel.Protocol = OtlpProtocol.Grpc;
            otel.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"]    = "MyApi",
                ["deployment.environment"] = ctx.HostingEnvironment.EnvironmentName,
            };
        })
);
```

---

## appsettings.json for Serilog

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore.Hosting": "Warning",
        "Microsoft.AspNetCore.Routing": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
        "Microsoft.EntityFrameworkCore.Infrastructure": "Warning",
        "Hangfire": "Warning",
        "System.Net.Http.HttpClient": "Warning"
      }
    },
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"],
    "Properties": {
      "Application": "MyApi"
    }
  }
}
```

---

## Application Insights Sink

```csharp
// Uses telemetry client from DI
.WriteTo.ApplicationInsights(
    services.GetRequiredService<TelemetryConfiguration>(),
    TelemetryConverter.Traces);
```

---

## Destructuring / Masking Sensitive Data

```csharp
// Mask sensitive properties from being logged
config.Destructure.ByTransforming<LoginRequest>(r =>
    new { r.Email, Password = "***REDACTED***" });

// Or use Destructurama policy
config.Destructure.UsingAttributes();  // Reads [NotLogged] and [LogMasked] attributes

// public record LoginRequest(
//     string Email,
//     [LogMasked(ShowFirst = 2)] string Password);
```

---

## Learn More

| Topic | Query |
|-------|-------|
| Serilog sinks | `microsoft_docs_search(query="Serilog sinks enrichers ASP.NET Core production configuration")` |
| Seq | `microsoft_docs_search(query="Seq log server Serilog structured logging development")` |
| Destructurama | `microsoft_docs_search(query="Serilog Destructurama attributed logging GDPR masking")` |
