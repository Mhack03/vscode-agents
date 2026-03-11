---
name: dotnet-testing
description: .NET testing patterns and best practices for .NET 8/9/10 applications. Use when writing unit tests, integration tests, or end-to-end tests in C# using xUnit, NUnit, MSTest, Moq, NSubstitute, FluentAssertions, WebApplicationFactory, TestContainers, Respawn (database reset between tests), Bogus (fake test data), bUnit (Blazor component testing), or BenchmarkDotNet. Covers Arrange-Act-Assert pattern, mocking, test fixtures, shared context with IClassFixture, integration testing ASP.NET Core Minimal APIs and controllers, testing EF Core with in-memory or real databases, snapshot testing, and test project structure for .NET solutions.
license: Complete terms in LICENSE.txt
---

# .NET Testing Patterns

Production testing patterns for .NET 8/9/10 covering unit tests, integration tests, Blazor component tests, and test infrastructure.

## When to Use This Skill

- Writing unit tests for services, domain logic, or application layer
- Setting up integration tests with `WebApplicationFactory` + `TestContainers`
- Testing ASP.NET Core Minimal API or Controller endpoints via HTTP
- Testing Blazor components with bUnit
- Mocking dependencies with NSubstitute or Moq
- Writing readable assertions with FluentAssertions
- Generating fake test data with Bogus
- Resetting database state between tests with Respawn
- Setting up test project structure in a .NET solution

## Prerequisites

```bash
dotnet new xunit -n MyApp.Tests
dotnet add package FluentAssertions
dotnet add package NSubstitute
dotnet add package Bogus
dotnet add package Microsoft.AspNetCore.Mvc.Testing    # WebApplicationFactory
dotnet add package Testcontainers.PostgreSql            # or .SqlServer, .MsSql
dotnet add package Respawn
dotnet add package bunit                               # Blazor component testing
```

## Unit Test Structure (xUnit)

```csharp
// Follow: Arrange → Act → Assert
// Name tests: MethodName_StateUnderTest_ExpectedBehavior

public class OrderServiceTests
{
    // ── Dependencies (substitutes, not mocks — prefer NSubstitute) ───
    private readonly IOrderRepository  _repo    = Substitute.For<IOrderRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly ILogger<OrderService> _log  = Substitute.For<ILogger<OrderService>>();

    // ── System Under Test ─────────────────────────────────────────────
    private readonly OrderService _sut;

    // ── Fake data builder ─────────────────────────────────────────────
    private readonly Faker<Order> _orderFaker = new Faker<Order>()
        .RuleFor(o => o.Id,         f => f.Random.Int(1, 10_000))
        .RuleFor(o => o.CustomerId, f => f.Random.Int(1, 1_000))
        .RuleFor(o => o.Status,     _ => OrderStatus.Pending)
        .RuleFor(o => o.CreatedAt,  f => f.Date.Recent(30));

    public OrderServiceTests() =>
        _sut = new OrderService(_repo, _customers, _log);

    [Fact]
    public async Task CreateOrderAsync_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var customer = new Customer { Id = 1, IsActive = true };
        _customers.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(customer);

        var request = new CreateOrderRequest(
            CustomerId: 1,
            Lines: [new OrderLineRequest(ProductId: 1, Quantity: 2)]);

        // Act
        var result = await _sut.CreateOrderAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOrderAsync_WithNoLines_ReturnsFailResult()
    {
        // Arrange
        var request = new CreateOrderRequest(CustomerId: 1, Lines: []);

        // Act
        var result = await _sut.CreateOrderAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("line");
    }

    [Fact]
    public async Task CreateOrderAsync_WithInactiveCustomer_ReturnsFailResult()
    {
        // Arrange
        var customer = new Customer { Id = 1, IsActive = false };
        _customers.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(customer);
        var request = new CreateOrderRequest(1, [new(1, 1)]);

        // Act
        var result = await _sut.CreateOrderAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("inactive");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task GetByIdAsync_WhenOrderExists_ReturnsOrder(int orderId)
    {
        // Arrange
        var order = _orderFaker.Clone()
            .RuleFor(o => o.Id, orderId)
            .Generate();
        _repo.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);

        // Act
        var result = await _sut.GetByIdAsync(orderId);

        // Assert
        result.Should().NotBeNull()
              .And.BeEquivalentTo(order, opts => opts.ExcludingMissingMembers());
    }
}
```

## Integration Test Infrastructure

```csharp
// Tests/Infrastructure/CustomWebApplicationFactory.cs
public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    // TestContainers — spins up a real Postgres container
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private Respawner    _respawner   = null!;
    private DbConnection _dbConn      = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Swap production DB for test container DB
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o =>
                o.UseNpgsql(_db.GetConnectionString()));

            // Replace external services
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService, FakeEmailService>();
        });
    }

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();

        _dbConn = new NpgsqlConnection(_db.GetConnectionString());
        await _dbConn.OpenAsync();

        _respawner = await Respawner.CreateAsync(_dbConn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres
        });
    }

    public async Task ResetDatabaseAsync() =>
        await _respawner.ResetAsync(_dbConn);

    public new async Task DisposeAsync()
    {
        await _dbConn.DisposeAsync();
        await _db.DisposeAsync();
    }
}

// Tests/Infrastructure/IntegrationTestBase.cs
public abstract class IntegrationTestBase(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    protected readonly HttpClient        Client   = factory.CreateClient();
    protected readonly IServiceProvider  Services = factory.Services;

    // Reset DB before each test
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync()    => Task.CompletedTask;

    // Seed helper — inserts entity directly via EF Core
    protected async Task<T> SeedAsync<T>(T entity) where T : class
    {
        using var scope   = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }
}
```

## API Endpoint Integration Tests

```csharp
public class ProductsApiTests(CustomWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GET_products_returns_200_with_list()
    {
        // Arrange
        await SeedAsync(new Product { Name = "Widget", Price = 9.99m });

        // Act
        var response = await Client.GetAsync("/api/v1/products");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        body.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task POST_products_with_valid_body_returns_201()
    {
        // Arrange
        var req = new CreateProductRequest("New Widget", 29.99m, "Tools");

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/products", req);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var created = await response.Content.ReadFromJsonAsync<ProductDto>();
        created!.Name.Should().Be("New Widget");
    }

    [Fact]
    public async Task POST_products_with_invalid_body_returns_400_problem_details()
    {
        // Arrange — empty name and negative price
        var req = new CreateProductRequest(string.Empty, -1m, string.Empty);

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/products", req);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Status.Should().Be(400);
    }
}
```

## Blazor Component Tests (bUnit)

```csharp
// Requires: dotnet add package bunit
public class ProductCardTests : TestContext
{
    [Fact]
    public void Renders_product_name_and_price()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Widget", Price = 9.99m };

        // Act
        var cut = RenderComponent<ProductCard>(p => p
            .Add(x => x.Product, product));

        // Assert
        cut.Find("h3").TextContent.Should().Contain("Widget");
        cut.Find(".price").TextContent.Should().Contain("9.99");
    }

    [Fact]
    public void Add_to_cart_button_fires_EventCallback()
    {
        // Arrange
        Product? captured = null;
        var product = new Product { Id = 1, Name = "Widget", Price = 9.99m };

        var cut = RenderComponent<ProductCard>(p => p
            .Add(x => x.Product, product)
            .Add(x => x.OnAddToCart, (Product p) => captured = p));

        // Act
        cut.Find("button.add-to-cart").Click();

        // Assert
        captured.Should().NotBeNull();
        captured!.Id.Should().Be(1);
    }

    [Fact]
    public void Shows_loading_spinner_while_fetching()
    {
        // Arrange — service that never completes
        var service = Substitute.For<IProductService>();
        service.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
               .Returns(new TaskCompletionSource<Product?>().Task);
        Services.AddSingleton(service);

        // Act
        var cut = RenderComponent<ProductDetail>(p => p.Add(x => x.Id, 1));

        // Assert
        cut.Find(".spinner-border").Should().NotBeNull();
    }
}
```

See [testcontainers-setup.md](references/testcontainers-setup.md) for SQL Server, Redis, and MongoDB containers.
See [mock-httpclient.md](references/mock-httpclient.md) for mocking `HttpClient` dependencies.
See [performance-testing.md](references/performance-testing.md) for BenchmarkDotNet setup.

## Quick Reference — Key Packages

| Package                            | Purpose                           |
| ---------------------------------- | --------------------------------- |
| `xunit`                            | Test runner and assertions        |
| `FluentAssertions`                 | Readable assertion syntax         |
| `NSubstitute`                      | Mocking/stubbing                  |
| `Bogus`                            | Fake data generation              |
| `Microsoft.AspNetCore.Mvc.Testing` | `WebApplicationFactory`           |
| `Testcontainers.PostgreSql`        | Real DB in Docker for tests       |
| `Respawn`                          | Fast database reset between tests |
| `bunit`                            | Blazor component testing          |
| `BenchmarkDotNet`                  | Micro-benchmarking                |
| `coverlet.collector`               | Code coverage collection          |

## Learn More

| Topic                 | How to Find                                                                            |
| --------------------- | -------------------------------------------------------------------------------------- |
| xUnit best practices  | `microsoft_docs_search(query="xunit .net unit testing best practices")`                |
| WebApplicationFactory | `microsoft_docs_search(query="WebApplicationFactory integration testing aspnet core")` |
| TestContainers .NET   | `microsoft_docs_search(query="testcontainers dotnet integration test database")`       |
| bUnit docs            | `microsoft_docs_fetch(url="https://bunit.dev/docs/getting-started/index.html")`        |
| FluentAssertions      | `microsoft_docs_search(query="FluentAssertions dotnet assertion library guide")`       |
| Code coverage         | `microsoft_docs_search(query="dotnet code coverage coverlet reportgenerator")`         |
