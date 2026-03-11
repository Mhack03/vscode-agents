---
name: blazor-architecture
description: Blazor application architecture, component patterns, and best practices for .NET 8/9/10. Use when building Blazor Server, Blazor WebAssembly, or Blazor Web App (Auto render mode) applications. Covers component lifecycle (OnInitializedAsync, OnParametersSetAsync, OnAfterRenderAsync), interactive render modes (InteractiveServer, InteractiveWebAssembly, InteractiveAuto), streaming rendering, enhanced navigation, forms and validation (EditForm, DataAnnotationsValidator, FluentValidation), cascading parameters, EventCallback, RenderFragment, JS interop (IJSRuntime), SignalR integration, authentication in Blazor (AuthorizeView, CascadingAuthenticationState), state management, or building reusable Blazor component libraries.
license: Complete terms in LICENSE.txt
---

# Blazor Architecture Patterns

Production patterns for Blazor .NET 8/9/10 covering render modes, component design, forms, interop, and authentication.

## When to Use This Skill

- Creating or modifying Blazor components (`.razor` files)
- Choosing a render mode (Server, WASM, Auto, SSR)
- Implementing component lifecycle correctly
- Building forms with validation (`EditForm`)
- Sharing state across components (cascading parameters, scoped services)
- JavaScript interop with `IJSRuntime`
- Authenticating and authorizing Blazor pages/components
- Streaming large data sets with `@attribute [StreamRendering]`
- Integrating real-time features with SignalR

## Prerequisites

```bash
dotnet new blazor -n MyApp --interactivity Auto   # Blazor Web App (.NET 8+)
dotnet new blazorwasm -n MyApp.Client             # Standalone WASM
```

## Render Mode Decision Guide

| Mode                     | Runs On          | Connection      | Best For                      |
| ------------------------ | ---------------- | --------------- | ----------------------------- |
| Static SSR (default)     | Server           | None            | Read-only pages, SEO          |
| `InteractiveServer`      | Server           | SignalR         | Admin, real-time, low latency |
| `InteractiveWebAssembly` | Browser          | None after load | Offline, no server cost       |
| `InteractiveAuto`        | Server → Browser | SignalR → None  | Best of both worlds           |

```razor
@* App.razor — apply globally *@
<Routes @rendermode="InteractiveAuto" />

@* Per-page override *@
@page "/dashboard"
@rendermode InteractiveServer

@* Static page with streaming *@
@page "/products"
@attribute [StreamRendering]
```

## Component Lifecycle

```razor
@implements IAsyncDisposable
@inject IProductService ProductService

@code {
    [Parameter, EditorRequired] public int Id { get; set; }
    [Parameter] public EventCallback<Product> OnSelected { get; set; }

    private Product? _product;
    private string?  _error;
    private readonly CancellationTokenSource _cts = new();

    // 1. Runs once when first rendered
    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    // 2. Runs every time a parameter value changes
    protected override async Task OnParametersSetAsync()
    {
        if (_product?.Id != Id)
            await LoadAsync();
    }

    // 3. After render — use for JS interop (firstRender == true on initial render)
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await JS.InvokeVoidAsync("highlightCode");
    }

    private async Task LoadAsync()
    {
        try
        {
            _product = await ProductService.GetByIdAsync(Id, _cts.Token);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
    }

    // Always implement IAsyncDisposable when holding CTS, JS modules, or event subs
    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
    }
}
```

## Forms & Validation

```razor
@page "/products/new"
@inject IProductService ProductService
@inject NavigationManager Nav

<EditForm Model="_model" OnValidSubmit="HandleSubmit" FormName="new-product">
    <DataAnnotationsValidator />
    <ValidationSummary class="alert alert-danger" />

    <div class="mb-3">
        <label class="form-label">Name</label>
        <InputText @bind-Value="_model.Name" class="form-control" />
        <ValidationMessage For="() => _model.Name" class="text-danger" />
    </div>

    <div class="mb-3">
        <label class="form-label">Price</label>
        <InputNumber @bind-Value="_model.Price" class="form-control" />
        <ValidationMessage For="() => _model.Price" class="text-danger" />
    </div>

    <div class="mb-3">
        <label class="form-label">Category</label>
        <InputSelect @bind-Value="_model.CategoryId" class="form-select">
            @foreach (var cat in _categories)
            {
                <option value="@cat.Id">@cat.Name</option>
            }
        </InputSelect>
    </div>

    <button type="submit" class="btn btn-primary" disabled="@_busy">
        @(_busy ? "Saving…" : "Create Product")
    </button>
</EditForm>

@code {
    [SupplyParameterFromForm]
    private CreateProductModel _model { get; set; } = new();

    private List<CategoryDto> _categories = [];
    private bool _busy;

    protected override async Task OnInitializedAsync()
    {
        _categories = await CategoryService.GetAllAsync();
    }

    private async Task HandleSubmit()
    {
        _busy = true;
        try
        {
            await ProductService.CreateAsync(_model);
            Nav.NavigateTo("/products", forceLoad: false);
        }
        finally { _busy = false; }
    }
}

// Validation model
public class CreateProductModel
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 99999.99)]
    public decimal Price { get; set; }

    [Required]
    public int CategoryId { get; set; }
}
```

## Component Communication

```razor
@* ── Parameter (parent → child) ────────────────────────────── *@
<ProductCard Product="product" IsHighlighted="true" />

@* ── EventCallback (child → parent) ────────────────────────── *@
@code {
    [Parameter, EditorRequired] public Product Product { get; set; } = default!;
    [Parameter] public bool IsHighlighted { get; set; }
    [Parameter] public EventCallback<Product> OnAddToCart { get; set; }

    private Task AddToCart() => OnAddToCart.InvokeAsync(Product);
}

@* ── RenderFragment (slot/template) ────────────────────────── *@
<Card>
    <Header><h3>Title</h3></Header>
    <Body><p>Content goes here</p></Body>
</Card>

@code {
    [Parameter] public RenderFragment? Header { get; set; }
    [Parameter] public RenderFragment? Body   { get; set; }
}

@* ── CascadingValue (deep tree sharing) ────────────────────── *@
<CascadingValue Value="_theme" Name="AppTheme">
    <Router AppAssembly="typeof(App).Assembly" />
</CascadingValue>

@code {
    // In any descendant
    [CascadingParameter(Name = "AppTheme")] public AppTheme Theme { get; set; } = default!;
}
```

## JavaScript Interop

```razor
@inject IJSRuntime JS
@implements IAsyncDisposable

@code {
    private IJSObjectReference? _module;
    private DotNetObjectReference<MyComponent>? _ref;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        // Lazy-load a JS module (avoids large upfront bundle)
        _module = await JS.InvokeAsync<IJSObjectReference>(
            "import", "./js/myComponent.js");

        _ref = DotNetObjectReference.Create(this);
        await _module.InvokeVoidAsync("initialize", _ref);
    }

    // Callable FROM JavaScript: DotNet.invokeMethodAsync('MyApp', 'HandleEvent', data)
    [JSInvokable]
    public void HandleEvent(string data) =>
        InvokeAsync(() => { /* update state */ StateHasChanged(); });

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("destroy");
            await _module.DisposeAsync();
        }
        _ref?.Dispose();
    }
}
```

## Authentication

```razor
@* Conditional UI *@
<AuthorizeView Policy="admin">
    <Authorized>   <button @onclick="Delete">Delete</button>  </Authorized>
    <NotAuthorized><p>No permission.</p>                       </NotAuthorized>
</AuthorizeView>

@* Page-level guard *@
@page "/admin"
@attribute [Authorize(Roles = "Admin")]

@* Programmatic check *@
@inject AuthenticationStateProvider AuthProvider

@code {
    private async Task<bool> IsAdminAsync()
    {
        var state = await AuthProvider.GetAuthenticationStateAsync();
        return state.User.IsInRole("Admin");
    }
}
```

See [render-modes.md](references/render-modes.md) for SSR, streaming, and hydration details.
See [state-management.md](references/state-management.md) for scoped services, Fluxor, and localStorage.
See [signalr-integration.md](references/signalr-integration.md) for real-time beyond Blazor Server.

## Learn More

| Topic               | How to Find                                                                                                     |
| ------------------- | --------------------------------------------------------------------------------------------------------------- |
| Blazor Web App      | `microsoft_docs_search(query="blazor web app render modes .net 8 overview")`                                    |
| Streaming rendering | `microsoft_docs_search(query="blazor streaming rendering StreamRendering attribute")`                           |
| Enhanced navigation | `microsoft_docs_search(query="blazor enhanced navigation enhanced form handling")`                              |
| Forms & validation  | `microsoft_docs_search(query="blazor EditForm DataAnnotationsValidator FluentValidation")`                      |
| JS Interop          | `microsoft_docs_fetch(url="https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/")` |
| Auth & identity     | `microsoft_docs_search(query="blazor authentication authorization .net 8 oidc")`                                |
| Component libraries | `microsoft_docs_search(query="blazor reusable class library components razor")`                                 |
| bUnit testing       | `microsoft_docs_search(query="bunit blazor component testing")`                                                 |
