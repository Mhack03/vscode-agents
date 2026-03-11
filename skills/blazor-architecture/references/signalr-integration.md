# SignalR Integration with Blazor

---

## Overview of Real-Time Options

| Scenario | Approach |
|----------|---------|
| Blazor Server | Built-in — UI already uses SignalR; just update component state |
| Blazor WASM / Static SSR | Manual HubConnection via `Microsoft.AspNetCore.SignalR.Client` |
| Push notifications | SignalR Hub + `IHubContext<T>` injected into services |
| Live dashboards | Streaming from server using `IAsyncEnumerable` or hub channels |

---

## Server-Side Hub Setup

```csharp
// Hubs/NotificationHub.cs
public class NotificationHub : Hub<INotificationClient>
{
    // Strongly-typed hub — compiler checks method names
    public async Task Subscribe(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
    }

    public async Task Unsubscribe(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
    }
}

// Client interface (server-to-client messages)
public interface INotificationClient
{
    Task ReceiveNotification(NotificationDto notification);
    Task ReceiveOrderUpdate(int orderId, string status);
}

// Registration (Program.cs)
builder.Services.AddSignalR();
app.MapHub<NotificationHub>("/hubs/notifications")
   .RequireAuthorization();
```

---

## Pushing from Service via IHubContext

```csharp
// Services/OrderService.cs
public class OrderService(
    IHubContext<NotificationHub, INotificationClient> hubContext,
    AppDbContext context)
{
    public async Task UpdateOrderStatusAsync(int orderId, string status, CancellationToken ct)
    {
        var order = await context.Orders.FindAsync([orderId], ct)
            ?? throw new NotFoundException(orderId);

        order.Status = status;
        await context.SaveChangesAsync(ct);

        // Push to the user's group
        await hubContext.Clients
            .Group($"user:{order.CustomerId}")
            .ReceiveOrderUpdate(orderId, status);
    }
}
```

---

## Blazor WASM / Static SSR Client

```razor
@* Components/LiveOrderStatus.razor *@
@implements IAsyncDisposable
@inject NavigationManager Nav

@if (_status is not null)
{
    <span class="badge">@_status</span>
}

@code {
    [Parameter, EditorRequired] public int OrderId { get; set; }

    private HubConnection? _hub;
    private string? _status;

    protected override async Task OnInitializedAsync()
    {
        _hub = new HubConnectionBuilder()
            .WithUrl(Nav.ToAbsoluteUri("/hubs/notifications"),
                opts =>
                {
                    // Attach access token (stored in memory/localStorage)
                    opts.AccessTokenProvider = GetAccessTokenAsync;
                })
            .WithAutomaticReconnect()
            .Build();

        _hub.On<int, string>("ReceiveOrderUpdate", (orderId, status) =>
        {
            if (orderId == OrderId)
            {
                _status = status;
                InvokeAsync(StateHasChanged);
            }
        });

        await _hub.StartAsync();
        await _hub.SendAsync("Subscribe", CurrentUserId);
    }

    private Task<string?> GetAccessTokenAsync()
    {
        // Retrieve from your auth service / localStorage
        return Task.FromResult<string?>(AuthService.AccessToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
            await _hub.DisposeAsync();
    }
}
```

---

## Server-Side Streaming (IAsyncEnumerable)

Stream data from server to clients without polling:

```csharp
// Hub method
public async IAsyncEnumerable<StockPrice> StreamPrices(
    string symbol,
    [EnumeratorCancellation] CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        yield return await priceService.GetPriceAsync(symbol, ct);
        await Task.Delay(500, ct);
    }
}
```

```razor
@* Blazor WASM client consuming stream *@
@code {
    protected override async Task OnInitializedAsync()
    {
        await foreach (var price in _hub!.StreamAsync<StockPrice>(
            "StreamPrices", "MSFT", CancellationToken))
        {
            _price = price;
            await InvokeAsync(StateHasChanged);
        }
    }
}
```

---

## Reconnection Handling

```csharp
_hub = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect(new[]
    {
        TimeSpan.FromSeconds(0),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(30)
    })
    .Build();

_hub.Closed   += async ex => { _state = "Disconnected"; await InvokeAsync(StateHasChanged); };
_hub.Reconnecting += ex => { _state = "Reconnecting..."; return InvokeAsync(StateHasChanged); };
_hub.Reconnected  += id => { _state = "Connected";       return InvokeAsync(StateHasChanged); };
```

---

## Learn More

| Topic | Query |
|-------|-------|
| SignalR hub | `microsoft_docs_fetch(url="https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs")` |
| Blazor + SignalR | `microsoft_docs_search(query="blazor signalr HubConnection WASM client real-time")` |
| IHubContext | `microsoft_docs_search(query="IHubContext SignalR push notifications from service")` |
| Server-side streaming | `microsoft_docs_search(query="SignalR server side streaming IAsyncEnumerable hub")` |
