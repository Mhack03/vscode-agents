# TestContainers Setup — .NET

TestContainers spins up real Docker containers for integration tests, ensuring test fidelity against production-equivalent databases and services.

---

## Installation

```bash
dotnet add package Testcontainers
dotnet add package Testcontainers.PostgreSql
dotnet add package Testcontainers.MsSql
dotnet add package Testcontainers.Redis
dotnet add package Testcontainers.MongoDb
dotnet add package Testcontainers.RabbitMq
```

Docker must be running on the test machine or CI agent.

---

## PostgreSQL

```csharp
public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync()    => _container.DisposeAsync().AsTask();
}

// Register in WebApplicationFactory
protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    builder.ConfigureTestServices(services =>
    {
        services.RemoveAll<DbContextOptions<AppDbContext>>();
        services.AddDbContext<AppDbContext>(o =>
            o.UseNpgsql(_pgFixture.ConnectionString));
    });
}
```

---

## SQL Server

```csharp
private readonly MsSqlContainer _mssql = new MsSqlBuilder()
    .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
    .WithPassword("YourStrong!Passw0rd")
    .Build();

// Connection string
var connStr = _mssql.GetConnectionString();
// Register: services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connStr));
```

---

## Redis (Distributed Cache / Output Cache)

```csharp
private readonly RedisContainer _redis = new RedisBuilder()
    .WithImage("redis:7-alpine")
    .Build();

// In ConfigureTestServices
services.RemoveAll<IConnectionMultiplexer>();
services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(_redis.GetConnectionString()));
services.AddStackExchangeRedisCache(o =>
    o.Configuration = _redis.GetConnectionString());
```

---

## MongoDB

```csharp
private readonly MongoDbContainer _mongo = new MongoDbBuilder()
    .WithImage("mongo:7")
    .Build();

services.Configure<MongoDbSettings>(opts =>
    opts.ConnectionString = _mongo.GetConnectionString());
```

---

## Shared Container Across Test Classes (ICollectionFixture)

Starting a container per class is slow. Share one across a collection:

```csharp
// Define the collection
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<SharedDatabaseFixture> { }

// Shared fixture
public class SharedDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine").Build();

    private Respawner _respawner = null!;
    private NpgsqlConnection _conn = null!;

    public string ConnectionString => _db.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        _conn = new NpgsqlConnection(ConnectionString);
        await _conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(_conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres
        });

        // Apply migrations once
        using var ctx = CreateDbContext();
        await ctx.Database.MigrateAsync();
    }

    public Task ResetAsync() => _respawner.ResetAsync(_conn);

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString).Options;
        return new AppDbContext(options);
    }

    public async Task DisposeAsync()
    {
        await _conn.DisposeAsync();
        await _db.DisposeAsync();
    }
}

// Use in test class
[Collection("Database")]
public class ProductRepositoryTests(SharedDatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();  // Clean slate per test
    public Task DisposeAsync()    => Task.CompletedTask;

    [Fact]
    public async Task AddProduct_PersistsToDatabase()
    {
        using var ctx = fixture.CreateDbContext();
        ctx.Products.Add(new Product { Name = "Test", Price = 9.99m });
        await ctx.SaveChangesAsync();

        var count = await ctx.Products.CountAsync();
        count.Should().Be(1);
    }
}
```

---

## CI / GitHub Actions Setup

```yaml
# .github/workflows/tests.yml
jobs:
  test:
    runs-on: ubuntu-latest   # Docker available on ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.x'

      - name: Run tests
        run: dotnet test --configuration Release --logger "trx;LogFileName=results.trx"
        # TestContainers auto-detects Docker on Linux — no manual Docker setup needed

      - name: Publish test results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Test Results
          path: '**/*.trx'
          reporter: dotnet-trx
```

---

## Learn More

| Topic | Query |
|-------|-------|
| TestContainers .NET | `microsoft_docs_search(query="testcontainers dotnet xunit integration test")` |
| Respawn | `microsoft_docs_search(query="Respawn database reset integration tests .net")` |
| GitHub Actions Docker | `microsoft_docs_search(query="github actions dotnet integration test docker testcontainers")` |
