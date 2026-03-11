# Performance Testing — .NET

Two main tools: **BenchmarkDotNet** for micro-benchmarks (CPU/memory) and **NBomber** for load/stress testing.

---

## BenchmarkDotNet — Micro-benchmarks

```bash
dotnet add package BenchmarkDotNet
```

```csharp
// Benchmarks/JsonSerializationBenchmarks.cs
[MemoryDiagnoser]           // Tracks allocations
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net80)]  // Compare across runtimes
public class JsonSerializationBenchmarks
{
    private static readonly Product _product =
        new(1, "Test Product", 29.99m, "A great product");

    private static readonly string _json =
        JsonSerializer.Serialize(_product);

    // Source-generated serializer context
    private static readonly ProductJsonContext _ctx = new();

    [Benchmark(Baseline = true)]
    public string Serialize_Reflection() =>
        JsonSerializer.Serialize(_product);

    [Benchmark]
    public string Serialize_SourceGen() =>
        JsonSerializer.Serialize(_product, _ctx.Product);

    [Benchmark]
    public Product? Deserialize_Reflection() =>
        JsonSerializer.Deserialize<Product>(_json);

    [Benchmark]
    public Product? Deserialize_SourceGen() =>
        JsonSerializer.Deserialize<Product>(_json, _ctx.Product);
}

// Program.cs for benchmarks project
BenchmarkRunner.Run<JsonSerializationBenchmarks>();
// Run: dotnet run -c Release
```

---

## BenchmarkDotNet — Setup & Advanced

```csharp
// Global setup
[GlobalSetup]
public void Setup()
{
    // Runs once before all benchmark iterations
    _data = Enumerable.Range(1, 10_000)
        .Select(i => new Product(i, $"Product {i}", i * 1.5m, ""))
        .ToList();
}

// Per-iteration setup (caution: affects measurements)
[IterationSetup]
public void IterationSetup() { }

// Parameterized benchmarks
[Params(100, 1000, 10_000)]
public int N;

[Benchmark]
public List<Product> FilterProducts() =>
    _data.Where(p => p.Price < N).ToList();

// Diagnosers
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
[TailCallDiagnoser]
[DisassemblyDiagnoser]
```

---

## BenchmarkDotNet — Reading Results

```
| Method                  | Runtime  | Mean     | Ratio | Allocated |
|-------------------------|----------|----------|-------|-----------|
| Serialize_Reflection    | .NET 9.0 | 245.3 ns | 1.00  | 320 B     |
| Serialize_SourceGen     | .NET 9.0 |  89.1 ns | 0.36  |  96 B     |
| Deserialize_Reflection  | .NET 9.0 | 312.8 ns | 1.00  | 488 B     |
| Deserialize_SourceGen   | .NET 9.0 | 112.4 ns | 0.36  | 152 B     |
```

- **Mean**: Average time per operation
- **Ratio**: vs Baseline (1.00 = same as baseline, lower = faster)
- **Allocated**: Heap allocations per operation (lower = better)

---

## NBomber — Load Testing

```bash
dotnet add package NBomber
dotnet add package NBomber.Http
```

```csharp
// LoadTests/ApiLoadTest.cs
var httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:5001") };

var scenario = Scenario.Create("get_products", async context =>
    {
        var response = await httpClient.GetAsync("/api/products");
        return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
    })
    .WithWarmUpDuration(TimeSpan.FromSeconds(5))
    .WithLoadSimulations(
        Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromSeconds(30)),
        Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(30))
    );

NBomberRunner
    .RegisterScenarios(scenario)
    .WithReportingInterval(TimeSpan.FromSeconds(10))
    .WithReportFolder("load-test-results")
    .Run();
```

---

## NBomber — Step-Based Scenario (Auth + CRUD)

```csharp
var scenario = Scenario.Create("order_workflow", async ctx =>
{
    // Step 1: Login
    var loginResp = await httpClient.PostAsJsonAsync("/api/auth/login",
        new { email = "test@example.com", password = "Test123!" });

    if (!loginResp.IsSuccessStatusCode) return Response.Fail("Login failed");

    var token = (await loginResp.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;

    // Step 2: Create order with auth token
    var orderReq = new HttpRequestMessage(HttpMethod.Post, "/api/orders");
    orderReq.Headers.Authorization = new("Bearer", token);
    orderReq.Content = JsonContent.Create(new { ProductId = 1, Quantity = 2 });

    var orderResp = await httpClient.SendAsync(orderReq);
    return orderResp.IsSuccessStatusCode
        ? Response.Ok(sizeBytes: (int)(orderResp.Content.Headers.ContentLength ?? 0))
        : Response.Fail($"Order failed: {orderResp.StatusCode}");
})
.WithLoadSimulations(
    Simulation.RampingInject(rate: 50, interval: TimeSpan.FromSeconds(1),
                              during: TimeSpan.FromSeconds(60))
);
```

---

## k6 (External Tool — CLI-based)

For production-like load testing from CI:

```javascript
// k6-script.js
import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  vus: 50, duration: "60s",
  thresholds: { "http_req_duration": ["p(95)<500"] },  // 95th percentile < 500ms
};

export default function () {
  const res = http.get("https://api.example.com/products");
  check(res, { "status is 200": (r) => r.status === 200 });
  sleep(1);
}
```

```bash
# Run: k6 run k6-script.js
```

---

## .NET Application Performance Tips

```csharp
// 1. Use ValueTask for hot paths to avoid Task allocation
public ValueTask<int> GetCountAsync() => ValueTask.FromResult(_count);

// 2. ArrayPool for large temporary buffers
var buffer = ArrayPool<byte>.Shared.Rent(4096);
try { /* use buffer */ }
finally { ArrayPool<byte>.Shared.Return(buffer); }

// 3. Span<T> and Memory<T> for zero-copy operations
public static int CountWords(ReadOnlySpan<char> text) =>
    text.IsEmpty ? 0 : text.Count(' ') + 1;

// 4. Source-generated JSON context
[JsonSerializable(typeof(Product))]
[JsonSerializable(typeof(List<Product>))]
public partial class ProductJsonContext : JsonSerializerContext { }
```

---

## Learn More

| Topic | Query |
|-------|-------|
| BenchmarkDotNet | `microsoft_docs_search(query="BenchmarkDotNet dotnet performance benchmark MemoryDiagnoser")` |
| NBomber | `microsoft_docs_search(query="NBomber load testing dotnet HTTP scenario")` |
| dotnet-trace | `microsoft_docs_search(query="dotnet-trace diagnostics performance profiling")` |
| System.Diagnostics.Metrics | `microsoft_docs_search(query="dotnet System.Diagnostics.Metrics IMeterFactory OpenTelemetry")` |
