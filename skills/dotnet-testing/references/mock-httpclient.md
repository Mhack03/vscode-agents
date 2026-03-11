# Mocking HttpClient in .NET Tests

`HttpClient` cannot be directly mocked with NSubstitute/Moq because it wraps `HttpMessageHandler`. Use these patterns instead.

---

## Pattern 1: MockHttpMessageHandler (NSubstitute)

```bash
dotnet add package RichardSzalay.MockHttp
```

```csharp
// Create a mock handler and configure responses
var mockHttp = new MockHttpMessageHandler();

mockHttp.When(HttpMethod.Get, "https://api.example.com/products")
        .Respond("application/json",
            """[{"id":1,"name":"Widget","price":9.99}]""");

mockHttp.When(HttpMethod.Get, "https://api.example.com/products/999")
        .Respond(HttpStatusCode.NotFound);

mockHttp.When(HttpMethod.Post, "https://api.example.com/products")
        .Respond(HttpStatusCode.Created,
            "application/json", """{"id":2,"name":"New Widget","price":19.99}""");

// Create typed client
var client = mockHttp.ToHttpClient();
client.BaseAddress = new Uri("https://api.example.com");

// Inject into service
var service = new ProductApiClient(client);

// Act
var products = await service.GetAllAsync();

// Assert
products.Should().HaveCount(1);
mockHttp.VerifyNoOutstandingRequest();
```

---

## Pattern 2: Typed HTTP Client in WebApplicationFactory

Register a mock handler in `ConfigureTestServices`:

```csharp
// The production TypedClient
public class StripeClient(HttpClient http) : IStripeClient
{
    public async Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount)
    {
        var response = await http.PostAsJsonAsync("/v1/payment_intents",
            new { amount = (int)(amount * 100), currency = "usd" });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaymentIntent>()
            ?? throw new InvalidOperationException();
    }
}

// In CustomWebApplicationFactory.ConfigureWebHost
builder.ConfigureTestServices(services =>
{
    var mockHttp = new MockHttpMessageHandler();

    mockHttp.When(HttpMethod.Post, "*/v1/payment_intents")
            .Respond("application/json",
                """{"id":"pi_test_123","status":"requires_payment_method"}""");

    // Replace the named HttpClient used by StripeClient
    services.AddHttpClient<IStripeClient, StripeClient>(client =>
        client.BaseAddress = new Uri("https://api.stripe.com"))
        .ConfigurePrimaryHttpMessageHandler(() => mockHttp);
});
```

---

## Pattern 3: IHttpClientFactory with DelegatingHandler

For more complex scenarios — intercept specific calls while forwarding others:

```csharp
public class FakeAuthHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        // Add fake auth header for tests
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");

        return base.SendAsync(request, ct);
    }
}

// Register in tests
services.AddTransient<FakeAuthHandler>();
services.AddHttpClient<IExternalApiClient, ExternalApiClient>()
        .AddHttpMessageHandler<FakeAuthHandler>();
```

---

## Pattern 4: WireMock.Net (Full HTTP Server Mock)

For end-to-end scenarios where you need a real HTTP server:

```bash
dotnet add package WireMock.Net
```

```csharp
public class WireMockFixture : IAsyncLifetime
{
    private WireMockServer _server = null!;
    public string BaseUrl => _server.Url!;

    public Task InitializeAsync()
    {
        _server = WireMockServer.Start();

        _server.Given(Request.Create()
                   .WithPath("/api/products")
                   .UsingGet())
               .RespondWith(Response.Create()
                   .WithStatusCode(200)
                   .WithBodyAsJson(new[]
                   {
                       new { id = 1, name = "Widget", price = 9.99 }
                   }));

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _server?.Stop();
        return Task.CompletedTask;
    }
}

// In WebApplicationFactory
builder.ConfigureTestServices(services =>
{
    services.Configure<ExternalApiOptions>(o =>
        o.BaseUrl = _wireMock.BaseUrl);
});
```

---

## Anti-patterns to Avoid

```csharp
// ❌ Don't mock HttpClient directly — it's not an interface
var mock = Substitute.For<HttpClient>();  // This won't work as expected

// ❌ Don't use real external APIs in unit/integration tests
// (They're slow, flaky, and may incur costs)

// ✅ Always mock external HTTP dependencies in tests
// ✅ Use integration tests (WireMock or real staging env) for E2E validation
```

---

## Learn More

| Topic | Query |
|-------|-------|
| MockHttp | `microsoft_docs_search(query="RichardSzalay MockHttp dotnet httpclient testing")` |
| WireMock.Net | `microsoft_docs_search(query="WireMock.Net dotnet integration testing HTTP mock server")` |
| Typed HttpClient testing | `microsoft_docs_search(query="ASP.NET Core typed httpclient testing WebApplicationFactory")` |
