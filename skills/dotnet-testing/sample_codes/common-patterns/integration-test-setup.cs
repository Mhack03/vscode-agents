// dotnet-testing/sample_codes/common-patterns/integration-test-setup.cs
// Full WebApplicationFactory + TestContainers + Respawner integration test setup

using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Bogus;
using Xunit;
using Respawn;
using Npgsql;

namespace MyApp.Tests.Integration;

// ── Shared Database Fixture (one container for the whole test suite) ──────────

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("testdb")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private Respawner _respawner = null!;
    private NpgsqlConnection _conn = null!;

    public string ConnectionString => _pg.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();

        // Apply migrations once for the whole collection
        _conn = new NpgsqlConnection(ConnectionString);
        await _conn.OpenAsync();

        using var scope = CreateServiceScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        _respawner = await Respawner.CreateAsync(_conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore = ["__EFMigrationsHistory"],
        });
    }

    /// <summary>Wipe all data between tests (fast, no DROP/CREATE).</summary>
    public Task ResetDatabaseAsync() => _respawner.ResetAsync(_conn);

    private IServiceScope CreateServiceScope()
    {
        var factory = new CustomWebAppFactory(ConnectionString);
        return factory.Services.CreateScope();
    }

    public async Task DisposeAsync()
    {
        await _conn.DisposeAsync();
        await _pg.DisposeAsync();
    }
}

// Share one Postgres container across all tests in "Integration" collection
[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<DatabaseFixture> { }

// ── Custom WebApplicationFactory ──────────────────────────────────────────────

public class CustomWebAppFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Replace production DB with test container
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o =>
                o.UseNpgsql(connectionString));

            // Replace real external service clients with fakes
            services.RemoveAll<IPaymentGateway>();
            services.AddSingleton<IPaymentGateway, FakePaymentGateway>();

            // Replace email sender
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, FakeEmailSender>();
        });
    }
}

// ── Integration Test Base Class ───────────────────────────────────────────────

[Collection("Integration")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly DatabaseFixture Db;
    protected readonly HttpClient Client;
    private readonly CustomWebAppFactory _factory;

    protected IntegrationTestBase(DatabaseFixture db)
    {
        Db = db;
        _factory = new CustomWebAppFactory(db.ConnectionString);
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });
    }

    public Task InitializeAsync() => Db.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        Client.Dispose();
        return _factory.DisposeAsync().AsTask();
    }

    // Helper to seed data using the test DbContext
    protected async Task SeedAsync(Action<AppDbContext> seed)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        seed(ctx);
        await ctx.SaveChangesAsync();
    }

    // Helper to authenticate — sets Authorization header for subsequent requests
    protected async Task AuthenticateAsync(string role = "User")
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new { email = TestUsers.GetEmail(role), password = "Test123!" });
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", tokenResponse!.AccessToken);
    }
}

// ── Concrete Test Class ───────────────────────────────────────────────────────

public class ProductsEndpointTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    private static readonly Faker<Product> ProductFaker = new Faker<Product>()
        .RuleFor(p => p.Name, f => f.Commerce.ProductName())
        .RuleFor(p => p.Price, f => f.Finance.Amount(1m, 500m))
        .RuleFor(p => p.IsActive, _ => true);

    [Fact]
    public async Task GET_products_Returns200_WithActiveProducts()
    {
        // Arrange — seed 3 active products + 1 inactive
        await SeedAsync(ctx =>
        {
            ctx.Products.AddRange(ProductFaker.Generate(3));
            ctx.Products.Add(ProductFaker.Generate() with { IsActive = false });
        });

        // Act
        var response = await Client.GetAsync("/api/v1/products");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(3);  // Global query filter hides inactive
        result.Items.Should().AllSatisfy(p =>
        {
            p.Id.Should().BePositive();
            p.Name.Should().NotBeNullOrEmpty();
            p.Price.Should().BePositive();
        });
    }

    [Fact]
    public async Task POST_products_WithValidData_Returns201_WithLocation()
    {
        // Arrange
        await AuthenticateAsync("Admin");

        var request = new
        {
            name = "Widget Pro",
            price = 29.99,
            category = "Electronics",
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/products", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var created = await response.Content.ReadFromJsonAsync<ProductDto>();
        created!.Name.Should().Be("Widget Pro");
        created.Price.Should().Be(29.99m);
    }

    [Fact]
    public async Task POST_products_Unauthenticated_Returns401()
    {
        // Act (no AuthenticateAsync call)
        var response = await Client.PostAsJsonAsync("/api/v1/products",
            new { name = "Test", price = 10.00, category = "Misc" });

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DELETE_product_WhenNotAdmin_Returns403()
    {
        // Arrange
        await AuthenticateAsync("User");  // Regular user, not Admin
        await SeedAsync(ctx => ctx.Products.Add(ProductFaker.Generate() with { Id = 100 }));

        // Act
        var response = await Client.DeleteAsync("/api/v1/products/100");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }
}
