# Blazor HTTP Client Integration

## ApiClient Service

```csharp
// Services/ApiClient.cs
using System.Net.Http.Json;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly TokenService _tokenService;

    public ApiClient(HttpClient httpClient, TokenService tokenService)
    {
        _httpClient = httpClient;
        _tokenService = tokenService;
    }

    private async Task<T> SendRequestAsync<T>(
        HttpMethod method,
        string endpoint,
        object? requestBody = null)
    {
        var request = new HttpRequestMessage(method, endpoint);

        // Add auth token
        var token = await _tokenService.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new("Bearer", token);
        }

        if (requestBody != null)
        {
            request.Content = JsonContent.Create(requestBody);
        }

        var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Try to refresh token
            var refreshSuccess = await _tokenService.RefreshTokenAsync();
            if (refreshSuccess)
            {
                return await SendRequestAsync<T>(method, endpoint, requestBody);
            }
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsAsync<T>();
    }

    public Task<T> GetAsync<T>(string endpoint)
        => SendRequestAsync<T>(HttpMethod.Get, endpoint);

    public Task<T> PostAsync<T>(string endpoint, object requestBody)
        => SendRequestAsync<T>(HttpMethod.Post, endpoint, requestBody);

    public Task<T> PutAsync<T>(string endpoint, object requestBody)
        => SendRequestAsync<T>(HttpMethod.Put, endpoint, requestBody);

    public Task DeleteAsync(string endpoint)
        => SendRequestAsync<object>(HttpMethod.Delete, endpoint);
}
```

## Blazor Component Usage

```csharp
// Pages/Users.razor
@page "/users"
@inject UserService UserService
@implements IAsyncDisposable

<div>
    @if (Users == null)
    {
        <p>Loading...</p>
    }
    else if (Error != null)
    {
        <p>Error: @Error</p>
    }
    else
    {
        <table>
            @foreach (var user in Users)
            {
                <tr>
                    <td>@user.Name</td>
                    <td>@user.Email</td>
                </tr>
            }
        </table>
    }
</div>

@code {
    private List<User>? Users;
    private string? Error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            Users = await UserService.GetUsersAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }
}
```

## Registration in Program.cs

```csharp
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5000/api");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<UserService>();
```
