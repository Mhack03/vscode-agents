---
name: dotnet-background-jobs
description: Background processing for .NET 8/9/10 — IHostedService, BackgroundService, Hangfire scheduled jobs, MassTransit with RabbitMQ/Azure Service Bus, and the outbox pattern.
license: Complete terms in LICENSE.txt
---

# .NET Background Jobs & Messaging

## When to Use This Skill

- Running periodic tasks (cleanup, reports, reminders) with Hangfire or Quartz.NET
- Processing long-running work off the HTTP request thread
- Reliable async messaging with MassTransit + RabbitMQ or Azure Service Bus
- Implementing the outbox pattern to prevent message loss on DB + message publish
- Fire-and-forget background jobs (email, notifications)
- Worker Service projects for standalone background processors

---

## IHostedService / BackgroundService

For simple start/stop or periodic tasks without an external dependency:

```csharp
// Simple fire-and-forget background service
public class DatabaseMigratorService(
    IServiceProvider sp,
    ILogger<DatabaseMigratorService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        logger.LogInformation("Applying pending migrations...");
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

// Periodic background service
public class CacheWarmupService(
    IServiceProvider sp,
    ILogger<CacheWarmupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = sp.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IProductService>();
                await svc.WarmCacheAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Cache warmup failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}

// Registration
builder.Services.AddHostedService<DatabaseMigratorService>();
builder.Services.AddHostedService<CacheWarmupService>();
```

---

## Hangfire — Scheduled & Recurring Jobs

```bash
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.SqlServer   # or Hangfire.PostgreSql / Hangfire.Redis
```

```csharp
// Program.cs
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("Hangfire")));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 2;
    options.Queues      = ["critical", "default", "low"];
});

app.MapHangfireDashboard("/jobs",
    new DashboardOptions { IsReadOnlyFunc = _ => !env.IsDevelopment() })
   .RequireAuthorization("AdminOnly");

// Enqueue jobs
BackgroundJob.Enqueue<IEmailService>(svc => svc.SendWelcomeEmailAsync(userId));

// Delayed job
BackgroundJob.Schedule<IEmailService>(
    svc => svc.SendReminderAsync(orderId),
    TimeSpan.FromHours(24));

// Recurring job — cron expression
RecurringJob.AddOrUpdate<IReportService>(
    "daily-report",
    svc => svc.GenerateDailyReportAsync(CancellationToken.None),
    Cron.Daily(9, 0),  // 9:00 AM UTC
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
```

---

## MassTransit — Messages & Events

```bash
dotnet add package MassTransit
dotnet add package MassTransit.RabbitMQ          # or MassTransit.Azure.ServiceBus.Core
dotnet add package MassTransit.EntityFrameworkCore  # for Outbox
```

```csharp
// Program.cs
builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<AppDbContext>(o =>
    {
        o.UseSqlServer();               // Stores messages in DB before publishing
        o.DisableInboxCleanupService(); // Use default cleanup
    });

    x.AddConsumers(typeof(Program).Assembly);  // Auto-registers all IConsumer<T>

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMq"), h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.UseMessageRetry(r => r.Exponential(5,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(5)));

        cfg.ConfigureEndpoints(ctx);  // Auto-maps consumer → queue names
    });
});
```

---

## Messages / Events (contracts)

```csharp
// Contracts/OrderMessages.cs — shared between publisher and consumer
namespace MyApp.Contracts;

public record OrderCreated(
    Guid     OrderId,
    string   CustomerId,
    decimal  Total,
    DateTime OccurredAt);

public record OrderShipped(
    Guid     OrderId,
    string   TrackingNumber,
    DateTime ShippedAt);
```

---

## Publishing Events

```csharp
public class OrderService(AppDbContext context, IPublishEndpoint publishEndpoint)
{
    public async Task<Order> CreateAsync(CreateOrderRequest req, CancellationToken ct)
    {
        var order = new Order { /* ... */ };
        context.Orders.Add(order);

        // Outbox: message is saved to DB in same transaction as order
        // Published to RabbitMQ asynchronously by MassTransit outbox worker
        await publishEndpoint.Publish(new OrderCreated(
            order.Id, order.CustomerId, order.Total, DateTime.UtcNow), ct);

        await context.SaveChangesAsync(ct);  // Order + outbox message saved atomically
        return order;
    }
}
```

---

## Consuming Events

```csharp
// Consumers/OrderCreatedConsumer.cs
public class OrderCreatedConsumer(
    IEmailService email,
    ILogger<OrderCreatedConsumer> logger) : IConsumer<OrderCreated>
{
    public async Task Consume(ConsumeContext<OrderCreated> ctx)
    {
        var msg = ctx.Message;
        logger.LogInformation("Processing order {OrderId}", msg.OrderId);

        await email.SendOrderConfirmationAsync(msg.CustomerId, msg.OrderId);
    }
}

// Consumer with fault handling
public class OrderCreatedConsumerDefinition : ConsumerDefinition<OrderCreatedConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpoint,
        IConsumerConfigurator<OrderCreatedConsumer> consumer,
        IRegistrationContext context)
    {
        endpoint.UseMessageRetry(r => r.Intervals(100, 500, 2000));
        endpoint.UseEntityFrameworkOutbox<AppDbContext>(context);
    }
}
```

---

## Outbox Pattern — Why It Matters

```
Without outbox:                    With outbox:
─────────────                      ────────────────────────
DB save ✅                          DB save ✅ (order + message together)
Publish fails ❌ → order created   Worker publishes ✅ (retries if needed)
but event never sent               Message delivered exactly once
```

---

## References

| Topic                                                    | Load When                                                             |
| -------------------------------------------------------- | --------------------------------------------------------------------- |
| [Hangfire Advanced](references/hangfire-advanced.md)     | Job continuations, batches, recurring schedules, dashboard auth       |
| [MassTransit & Outbox](references/masstransit-outbox.md) | Saga state machines, InMemory for tests, Azure Service Bus setup      |
| [Worker Service Pattern](references/worker-service.md)   | Standalone Worker Service, IHostedService patterns, graceful shutdown |

## Learn More

| Topic              | Query                                                                                                  |
| ------------------ | ------------------------------------------------------------------------------------------------------ |
| BackgroundService  | `microsoft_docs_fetch(url="https://learn.microsoft.com/en-us/dotnet/core/extensions/hosted-services")` |
| Hangfire           | `microsoft_docs_search(query="Hangfire ASP.NET Core recurring jobs scheduled fire-and-forget")`        |
| MassTransit outbox | `microsoft_docs_search(query="MassTransit EF Core outbox pattern transactional messaging")`            |
| Worker Service     | `microsoft_docs_search(query="dotnet Worker Service IHostedService BackgroundService .NET 9")`         |
