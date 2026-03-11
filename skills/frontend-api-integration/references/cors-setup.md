# CORS Configuration

## Browser CORS Preflight

CORS preflight requests are automatically sent by browsers for:

- Non-simple HTTP methods (PUT, DELETE, PATCH)
- Custom headers other than standard ones
- Content-Type other than form/text

## ASP.NET Core CORS Setup

```csharp
// Program.cs - Detailed CORS configuration
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    // Specific policy for frontend
    options.AddPolicy("FrontendDevelopment", policyBuilder =>
    {
        policyBuilder
            .WithOrigins("http://localhost:3000", "http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("X-Total-Count", "X-Page-Count", "X-Has-More");
    });

    // Strict policy for production
    options.AddPolicy("ProductionPolicy", policyBuilder =>
    {
        policyBuilder
            .WithOrigins("https://example.com", "https://app.example.com")
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithHeaders("Authorization", "Content-Type")
            .AllowCredentials();
    });
});

builder.Services.AddControllers();
var app = builder.Build();

// Apply CORS before routing
app.UseCors("FrontendDevelopment");
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

## Controller-Level CORS

```csharp
[ApiController]
[Route("api/[controller]")]
[EnableCors("FrontendDevelopment")]
public class UsersController : ControllerBase
{
    // Methods inherit CORS policy
}

// Or override for specific method
[HttpDelete("{id}")]
[EnableCors("AdminOnly")]
public IActionResult DeleteUser(int id) { }

// Or disable CORS
[HttpGet("public")]
[DisableCors]
public IActionResult GetPublicData() { }
```

## Development vs Production CORS

```csharp
var builder = WebApplication.CreateBuilder(args);

var corsPolicy = builder.Environment.IsDevelopment()
    ? "DevelopmentPolicy"
    : "ProductionPolicy";

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
    );

    options.AddPolicy("ProductionPolicy", p =>
        p.WithOrigins(builder.Configuration["AllowedOrigins"])
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
    );
});

var app = builder.Build();
app.UseCors(corsPolicy);
```
