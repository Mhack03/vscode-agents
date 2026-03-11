# Blazor Render Modes — Deep Dive

Understanding when and how Blazor renders content is critical to building performant, correct applications.

---

## Rendering Models Overview

| Mode | Annotation | Where Runs | Persistent Connection | Initial Load |
|------|------------|------------|----------------------|-------------|
| Static SSR | *(none)* | Server | None | Fast |
| Streaming SSR | `[StreamRendering]` | Server | None | Progressive |
| Interactive Server | `@rendermode InteractiveServer` | Server | SignalR (WebSocket) | Fast |
| Interactive WASM | `@rendermode InteractiveWebAssembly` | Browser | None after load | Slow (WASM download) |
| Interactive Auto | `@rendermode InteractiveAuto` | Server → Browser | SignalR → None | Fast then progressive |

---

## Static SSR (Default)

No `@rendermode` directive. Page renders once on server, sent as HTML.

```razor
@page "/products"
@inject IProductService ProductService

<h1>Products</h1>

@foreach (var p in _products)
{
    <div>@p.Name — @p.Price.ToString("C")</div>
}

@code {
    private IReadOnlyList<ProductDto> _products = [];

    protected override async Task OnInitializedAsync()
    {
        _products = await ProductService.GetAllAsync();
    }
}
```

- No JavaScript interactivity (buttons won't respond to clicks without Enhanced Navigation)
- Best for SEO, public pages, read-only dashboards

---

## Streaming Rendering

```razor
@page "/slow-page"
@attribute [StreamRendering]

<h1>Loading...</h1>

@if (_data is null)
{
    <p>Fetching data…</p>
}
else
{
    <DataGrid Data="_data" />
}

@code {
    private List<ReportRow>? _data;

    protected override async Task OnInitializedAsync()
    {
        // Page HTML streams immediately with the loading placeholder
        // Then updates when data arrives — all without JavaScript
        _data = await ReportService.GetSlowReportAsync();
    }
}
```

Key points:
- Server completes HTTP response progressively using chunked transfer encoding
- Great for data-heavy pages that need fast Time-to-First-Byte
- No persistent connection needed
- Requires SSR context — won't work with `@rendermode InteractiveServer`

---

## Interactive Server

```razor
@page "/counter"
@rendermode InteractiveServer

<h1>Counter: @_count</h1>
<button @onclick="Increment">+1</button>

@code {
    private int _count;
    void Increment() => _count++;
}
```

- Uses SignalR WebSocket — server state is maintained between renders
- Fast startup (no WASM download)
- Requires persistent server connection — poor for high-scale or serverless

---

## Interactive WASM

```razor
@page "/calculator"
@rendermode InteractiveWebAssembly

<h1>Calc: @_result</h1>
<button @onclick="Compute">Calculate</button>

@code {
    private double _result;
    void Compute() => _result = Math.Sqrt(2);  // Runs in browser
}
```

- WASM bundle downloads on first visit (~5–10 MB compressed)
- No server connection after load — runs entirely in browser
- Best for offline scenarios, CPU-intensive client logic, or reducing server load

---

## Interactive Auto

```razor
@page "/dashboard"
@rendermode InteractiveAuto
```

- First visit: SignalR (fast startup, no WASM download needed)
- Subsequent visits: WASM runs directly in browser (WASM cached)
- Best of both worlds for SPAs and authenticated apps

---

## Per-Component Override

Set a default render mode at the `Routes` level but override per component:

```razor
@* App.razor — default for all pages *@
<Routes @rendermode="InteractiveAuto" />

@* Override for a specific page *@
@page "/heavy-ssr"
@rendermode null        @* Forces Static SSR even with a global Interactive default *@
@attribute [StreamRendering]
```

---

## Pre-rendering

Interactive components render HTML on server first (pre-render), then "hydrate" (connect to JS/WASM/SignalR). This gives fast first paint.

```csharp
// Disable pre-rendering when it causes issues (e.g., reading cookies in OnInitializedAsync)
@rendermode @(new InteractiveServerRenderMode(prerender: false))
```

Pre-rendering means `OnInitializedAsync` runs **twice** — once on server (static), once on client (interactive). Guard side effects:

```razor
@code {
    protected override async Task OnInitializedAsync()
    {
        // RendererInfo.IsInteractive == false during pre-render
        if (RendererInfo.IsInteractive)
            await LoadExpensiveDataAsync();
    }
}
```

---

## Enhanced Navigation

.NET 8+ Blazor Web App uses enhanced navigation by default — page transitions happen without full reloads (SPA-style) even for static SSR pages.

```html
<!-- Disable on specific link -->
<a href="/external" data-enhance-nav="false">External</a>
```

```csharp
// Opt-out globally (rare)
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode()
   .DisableAntiforgery();  // Only if you know what you're doing
```

---

## Learn More

| Topic | Query |
|-------|-------|
| Render modes | `microsoft_docs_fetch(url="https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes")` |
| Streaming rendering | `microsoft_docs_search(query="blazor streaming rendering attribute OnInitializedAsync")` |
| Enhanced navigation | `microsoft_docs_search(query="blazor enhanced navigation enhanced form handling .net 8")` |
| Pre-rendering | `microsoft_docs_search(query="blazor prerendering interactive server statepersistence")` |
