// dotnet-testing/sample_codes/getting-started/unit-test-setup.cs
// Unit tests for a service using xUnit, NSubstitute, Bogus, and FluentAssertions

using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Bogus;
using Xunit;

namespace MyApp.Tests.Unit;

// ── Fake Data Factory (Bogus) ─────────────────────────────────────────────────

public static class FakeData
{
    public static readonly Faker<Product> Product = new Faker<Product>()
        .RuleFor(p => p.Id, f => f.Random.Int(1, 10_000))
        .RuleFor(p => p.Name, f => f.Commerce.ProductName())
        .RuleFor(p => p.Price, f => f.Finance.Amount(0.01m, 999.99m))
        .RuleFor(p => p.IsActive, _ => true);

    public static readonly Faker<CreateProductRequest> CreateProductRequest =
        new Faker<CreateProductRequest>()
            .CustomInstantiator(f => new CreateProductRequest(
                f.Commerce.ProductName(),
                f.Finance.Amount(1m, 500m),
                f.Commerce.Categories(1)[0],
                f.Lorem.Sentence()));
}

// ── Subject Under Test ────────────────────────────────────────────────────────

// ProductService depends on IProductRepository and IEventBus — both substituted
public class ProductServiceTests
{
    private readonly IProductRepository _repo = Substitute.For<IProductRepository>();
    private readonly IEventBus _events = Substitute.For<IEventBus>();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(_repo, _events);
    }

    // ── GetById ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ReturnsProductDto()
    {
        // Arrange
        var product = FakeData.Product.Generate();
        _repo.GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
             .Returns(product);

        // Act
        var result = await _sut.GetByIdAsync(product.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(product.Id);
        result.Name.Should().Be(product.Name);
        result.Price.Should().Be(product.Price);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductNotFound_ReturnsNull()
    {
        // Arrange
        _repo.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
             .Returns((Product?)null);

        // Act
        var result = await _sut.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    // ── Create ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesAndPublishesEvent()
    {
        // Arrange
        var request = FakeData.CreateProductRequest.Generate();
        var expected = FakeData.Product.Generate();

        _repo.AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>())
             .Returns(expected);

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        result.Id.Should().Be(expected.Id);

        // Verify the event was published exactly once with correct product ID
        await _events.Received(1)
            .PublishAsync(Arg.Is<ProductCreatedEvent>(e => e.ProductId == expected.Id),
                          Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", 100.00)]        // Empty name
    [InlineData("AB", 100.00)]        // Too short (< 3 chars)
    [InlineData("OK", -1.00)]         // Negative price
    [InlineData("OK", 0.00)]          // Zero price
    public async Task CreateAsync_WithInvalidInput_ThrowsValidationException(
        string name, decimal price)
    {
        // Arrange
        var request = new CreateProductRequest(name, price, "Electronics", null);

        // Act
        var act = () => _sut.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*invalid*");
    }

    // ── Error Handling ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        _repo.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
             .ThrowsAsync(new InvalidOperationException("DB connection failed"));

        // Act
        var act = () => _sut.GetByIdAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB connection failed");
    }

    // ── Cancellation ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WhenCancelled_ThrowsOperationCancelledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _repo.AddAsync(Arg.Any<Product>(), Arg.Is<CancellationToken>(ct => ct.IsCancellationRequested))
             .ThrowsAsync(new OperationCanceledException());

        var request = FakeData.CreateProductRequest.Generate();

        // Act
        var act = () => _sut.CreateAsync(request, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Verify no event was published on cancellation
        await _events.DidNotReceive()
            .PublishAsync(Arg.Any<ProductCreatedEvent>(), Arg.Any<CancellationToken>());
    }
}
