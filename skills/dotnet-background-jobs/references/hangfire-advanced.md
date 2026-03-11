# Hangfire Advanced Patterns

---

## Job Types Overview

```csharp
// 1. Fire-and-forget (one-time, async)
var jobId = BackgroundJob.Enqueue<IEmailService>(
    svc => svc.SendAsync(userId, "welcome", CancellationToken.None));

// 2. Scheduled (delayed one-time)
var jobId = BackgroundJob.Schedule<IReportService>(
    svc => svc.GenerateMonthlyAsync(DateTime.UtcNow, CancellationToken.None),
    TimeSpan.FromHours(1));

// 3. Continuations (chaining)
var step1 = BackgroundJob.Enqueue<IOrderService>(
    svc => svc.ReserveStockAsync(orderId, CancellationToken.None));

var step2 = BackgroundJob.ContinueJobWith<IOrderService>(step1,
    svc => svc.ChargePaymentAsync(orderId, CancellationToken.None));

BackgroundJob.ContinueJobWith<IEmailService>(step2,
    svc => svc.SendOrderConfirmationAsync(orderId, CancellationToken.None));

// 4. Recurring (cron)
RecurringJob.AddOrUpdate<IProductService>(
    "sync-catalog",
    svc => svc.SyncFromSupplierAsync(CancellationToken.None),
    Cron.Hourly());
```

---

## Multiple Queues (Priority)

```csharp
// Startup — configure server with queue priorities
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 20;
    options.Queues = ["critical", "default", "low"];  // Processed in order
});

// Enqueue to specific queue
[Queue("critical")]
public class NotificationJob
{
    public Task SendPasswordResetAsync(string userId) { /* ... */ }
}

// Or via attribute on method
BackgroundJob.Enqueue<INotificationJob>(
    x => x.SendPasswordResetAsync(userId));  // Uses [Queue] on class
```

---

## Job Filters (Cross-Cutting Concerns)

```csharp
// Log all job state transitions
public class JobLoggingFilter(ILogger<JobLoggingFilter> logger)
    : JobFilterAttribute, IServerFilter, IElectStateFilter
{
    public void OnPerforming(PerformingContext ctx)
        => logger.LogInformation("Starting job {JobId}: {JobType}",
            ctx.BackgroundJob.Id,
            ctx.BackgroundJob.Job.Type.Name);

    public void OnPerformed(PerformedContext ctx)
    {
        if (ctx.Exception is not null)
            logger.LogError(ctx.Exception,
                "Job {JobId} failed", ctx.BackgroundJob.Id);
        else
            logger.LogInformation("Job {JobId} succeeded in {Elapsed}ms",
                ctx.BackgroundJob.Id,
                (DateTime.UtcNow - ctx.BackgroundJob.CreatedAt).TotalMilliseconds);
    }

    public void OnStateElection(ElectStateContext ctx)
    {
        if (ctx.CandidateState is FailedState failed)
        {
            // Auto-delete jobs that have failed final retry
            ctx.CandidateState = new DeletedState { Reason = "Exhausted retries." };
        }
    }
}

// Register globally
GlobalJobFilters.Filters.Add(new JobLoggingFilter(logger));
```

---

## Dashboard Security

```csharp
// Restrict dashboard to Admin role
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization =
    [
        new HangfireAuthorizationFilter { Role = "Admin" }
    ],
    DashboardTitle    = "MyApi Background Jobs",
    StatsPollingInterval = 5000,  // ms
});

// Custom authorization filter
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public required string Role { get; init; }

    public bool Authorize(DashboardContext ctx)
    {
        var httpCtx = ctx.GetHttpContext();
        return httpCtx.User.Identity?.IsAuthenticated == true
            && httpCtx.User.IsInRole(Role);
    }
}
```

---

## Per-Job Retry Policies

```csharp
// Default global retry (10 attempts, exponential)
GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute
{
    Attempts = 10,
    DelaysInSeconds = [10, 30, 60, 120, 300],
    OnAttemptsExceeded = AttemptsExceededAction.Fail,
});

// Per-job override (no retry for payment — idempotency risk)
[AutomaticRetry(Attempts = 0)]
public class PaymentChargeJob
{
    public Task ExecuteAsync(Guid paymentId) { /* ... */ }
}
```

---

## Batches (Hangfire Pro)

```csharp
// Requires Hangfire.Pro
var batchId = BatchJob.StartNew(x =>
{
    x.Enqueue<IEmailService>(svc => svc.SendToUserAsync(user1Id));
    x.Enqueue<IEmailService>(svc => svc.SendToUserAsync(user2Id));
    x.Enqueue<IEmailService>(svc => svc.SendToUserAsync(user3Id));
});

// Run callback when all batch jobs complete
BatchJob.ContinueBatchWith(batchId, x =>
{
    x.Enqueue<IReportService>(svc => svc.MarkBatchSentAsync(batchId));
});
```

---

## Learn More

| Topic | Query |
|-------|-------|
| Hangfire | `microsoft_docs_search(query="Hangfire background jobs recurring continuations .NET configuration")` |
| Hangfire queues | `microsoft_docs_search(query="Hangfire multiple queues priority worker configuration")` |
| Hangfire filters | `microsoft_docs_search(query="Hangfire job filter IServerFilter IElectStateFilter")` |
