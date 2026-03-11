# State Management in Blazor

Choosing the right state management approach depends on component scope and lifetime.

---

## Decision Guide

| Scope | Storage | Approach |
|-------|---------|---------|
| Single component | Component fields | `private` fields, `StateHasChanged()` |
| Parent → children | Props | `[Parameter]`, `CascadingValue` |
| Sibling components | Shared service | Scoped service with events |
| Whole app (session) | Scoped service | `IJSRuntime` + `localStorage` |
| Cross-session | Server | Database / distributed cache |
| Complex global state | Library | Fluxor (Redux-like) |

---

## Scoped Service as State Container

The most common pattern for shared state in Blazor Server:

```csharp
// Services/CartService.cs
public class CartService
{
    private readonly List<CartItem> _items = [];

    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();
    public int Count => _items.Count;
    public decimal Total => _items.Sum(i => i.Price * i.Quantity);

    // Event so components can re-render when state changes
    public event Action? OnChange;

    public void AddItem(Product product, int quantity = 1)
    {
        var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing is not null)
            existing.Quantity += quantity;
        else
            _items.Add(new CartItem(product.Id, product.Name, product.Price, quantity));

        NotifyStateChanged();
    }

    public void RemoveItem(int productId)
    {
        _items.RemoveAll(i => i.ProductId == productId);
        NotifyStateChanged();
    }

    public void Clear()
    {
        _items.Clear();
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

// Register as Scoped — one instance per SignalR circuit (Blazor Server)
// or per WASM session (Blazor WASM)
builder.Services.AddScoped<CartService>();
```

```razor
@* CartIcon.razor — subscribes to state changes *@
@implements IDisposable
@inject CartService Cart

<span class="cart-count">@Cart.Count</span>

@code {
    protected override void OnInitialized()
    {
        Cart.OnChange += OnCartChanged;
    }

    // Ensure we re-render on non-Blazor thread (e.g., background service update)
    private void OnCartChanged() => InvokeAsync(StateHasChanged);

    public void Dispose() => Cart.OnChange -= OnCartChanged;
}
```

---

## localStorage / sessionStorage via JS Interop

For persisting state across browser refreshes:

```csharp
// Services/BrowserStorageService.cs
public class BrowserStorageService(IJSRuntime js)
{
    public ValueTask SetItemAsync<T>(string key, T value) =>
        js.InvokeVoidAsync("localStorage.setItem", key,
            JsonSerializer.Serialize(value));

    public async ValueTask<T?> GetItemAsync<T>(string key)
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }

    public ValueTask RemoveItemAsync(string key) =>
        js.InvokeVoidAsync("localStorage.removeItem", key);
}

builder.Services.AddScoped<BrowserStorageService>();
```

```razor
@inject BrowserStorageService Storage

@code {
    private UserPreferences _prefs = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // localStorage is only accessible after first render (requires JS interop)
        if (firstRender)
        {
            _prefs = await Storage.GetItemAsync<UserPreferences>("prefs") ?? new();
            StateHasChanged();
        }
    }

    private async Task SavePreferences()
    {
        await Storage.SetItemAsync("prefs", _prefs);
    }
}
```

---

## PersistentComponentState (Pre-render → Interactive Handoff)

Passes state computed during SSR pre-render to the interactive component, avoiding a second data fetch:

```razor
@implements IDisposable
@inject PersistentComponentState AppState

@code {
    private PersistingComponentStateSubscription _subscription;
    private WeatherForecast[]? _forecasts;

    protected override async Task OnInitializedAsync()
    {
        _subscription = AppState.RegisterOnPersisting(Persist);

        if (!AppState.TryTakeFromJson<WeatherForecast[]>("forecasts", out var restored))
            _forecasts = await WeatherService.GetForecastAsync();
        else
            _forecasts = restored;
    }

    private Task Persist()
    {
        AppState.PersistAsJson("forecasts", _forecasts);
        return Task.CompletedTask;
    }

    public void Dispose() => _subscription.Dispose();
}
```

---

## Fluxor (Redux-like Global State)

For large apps that need predictable unidirectional state flow:

```bash
dotnet add package Fluxor.Blazor.Web
```

```csharp
// State
[FeatureState]
public record CartState(ImmutableList<CartItem> Items)
{
    public CartState() : this(ImmutableList<CartItem>.Empty) { }
    public decimal Total => Items.Sum(i => i.Price * i.Quantity);
}

// Actions
public record AddToCartAction(CartItem Item);
public record RemoveFromCartAction(int ProductId);

// Reducer (pure function — no side effects)
public static class CartReducers
{
    [ReducerMethod]
    public static CartState OnAddToCart(CartState state, AddToCartAction action) =>
        state with { Items = state.Items.Add(action.Item) };

    [ReducerMethod]
    public static CartState OnRemoveFromCart(CartState state, RemoveFromCartAction action) =>
        state with { Items = state.Items.RemoveAll(i => i.ProductId == action.ProductId) };
}
```

```razor
@inherits Fluxor.Blazor.Web.Components.FluxorComponent
@inject IState<CartState> CartState
@inject IDispatcher Dispatcher

<p>Items: @CartState.Value.Items.Count</p>
<button @onclick="() => Dispatcher.Dispatch(new RemoveFromCartAction(1))">Remove</button>
```

---

## Learn More

| Topic | Query |
|-------|-------|
| Blazor state management | `microsoft_docs_search(query="blazor state management scoped service circuit")` |
| PersistentComponentState | `microsoft_docs_search(query="blazor PersistentComponentState prerender")` |
| Fluxor | `microsoft_docs_search(query="Fluxor blazor redux state management")` |
