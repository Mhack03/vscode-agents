// jobs-setup.cs — Hangfire + MassTransit Program.cs setup
// Demonstrates: SqlServer Hangfire, RabbitMQ MassTransit with outbox

var builder = WebApplication.CreateBuilder(args);

// ─── Hangfire ─────────────────────────────────────────────────────────────────
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(
        builder.Configuration.GetConnectionString("Sql"),
        new Hangfire.SqlServer.SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseExpirationManager = true,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true,
        }));

builder.Services.AddHangfireServer(opts =>
{
    opts.WorkerCount = Environment.ProcessorCount * 5;
    opts.Queues = ["critical", "default", "low"];
});

// ─── MassTransit + Outbox ─────────────────────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    // Transactional outbox — messages saved to DB atomically with business data
    x.AddEntityFrameworkOutbox<AppDbContext>(o =>
    {
        o.UseSqlServer();
        o.UseBusOutbox();
        o.QueryDelay = TimeSpan.FromSeconds(5);
    });

    // Auto-register all consumers in this assembly
    x.AddConsumers(typeof(Program).Assembly);

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMq")!);

        // Global retry policy — exponential back-off
        cfg.UseMessageRetry(r => r.Exponential(
            retryLimit: 5,
            minInterval: TimeSpan.FromMilliseconds(250),
            maxInterval: TimeSpan.FromSeconds(30),
            intervalDelta: TimeSpan.FromMilliseconds(250)));

        // Automatically configure consumer endpoints
        cfg.ConfigureEndpoints(ctx);
    });
});

// ─── Background services ──────────────────────────────────────────────────────
// Run DB migrations on startup
builder.Services.AddHostedService<DatabaseMigratorService>();

var app = builder.Build();

// ─── Hangfire Dashboard ──────────────────────────────────────────────────────
app.MapHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    Authorization = [new HangfireAuthorizationFilter()],
    DashboardTitle = "MyApi Jobs",
})
.RequireAuthorization("admin");

// ─── Recurring Jobs ───────────────────────────────────────────────────────────
RecurringJob.AddOrUpdate<IReportService>(
    "daily-report",
    svc => svc.GenerateDailyReportAsync(CancellationToken.None),
    Cron.Daily(hour: 2));   // 02:00 UTC

RecurringJob.AddOrUpdate<IProductService>(
    "sync-catalog",
    svc => svc.SyncFromSupplierAsync(CancellationToken.None),
    Cron.Hourly());

app.Run();

// ─── Hangfire Auth Filter ─────────────────────────────────────────────────────
public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext ctx) =>
        ctx.GetHttpContext().User.IsInRole("Admin");
}

// ─── DB Migration Service ─────────────────────────────────────────────────────
public class DatabaseMigratorService(IServiceScopeFactory scope,
    ILogger<DatabaseMigratorService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        logger.LogInformation("Running EF Core migrations...");
        await using var s = scope.CreateAsyncScope();
        var db = s.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(ct);
        logger.LogInformation("Migrations complete.");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
