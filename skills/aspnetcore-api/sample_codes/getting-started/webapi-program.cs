// aspnetcore-api/sample_codes/getting-started/webapi-program.cs
// Full ASP.NET Core 9 Web API Program.cs with all middleware configured

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using MyApi.Endpoints;
using MyApi.Exceptions;
using MyApi.Infrastructure;
using MyApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ── OpenAPI / Scalar ────────────────────────────────────────────────────────
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// ── Problem Details ─────────────────────────────────────────────────────────
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ── Authentication & Authorization ──────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("CanWriteProducts", p =>
        p.RequireAuthenticatedUser().RequireClaim("permissions", "products:write"));
});

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure(3)));

// ── Application Services ─────────────────────────────────────────────────────
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// ── Caching ──────────────────────────────────────────────────────────────────
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(b => b.Expire(TimeSpan.FromSeconds(60)));
    options.AddPolicy("Products", b =>
        b.Expire(TimeSpan.FromMinutes(5))
         .Tag("products")
         .SetVaryByQuery("page", "pageSize", "search"));
});

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.User.Identity?.Name ?? ctx.Request.Headers.Host.ToString(),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    options.AddPolicy("strict", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
            }));

    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await ctx.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = 429,
                Title = "Too Many Requests",
                Detail = "Rate limit exceeded. Please retry later.",
            }, ct);
    };
});

// ── CORS (for React + Vite dev server) ───────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("ViteDev", b =>
        b.WithOrigins("http://localhost:5173", "https://localhost:5173")
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());

    options.AddPolicy("Production", b =>
        b.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins")
             .Get<string[]>() ?? [])
         .AllowAnyHeader()
         .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
         .AllowCredentials());
});

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database")
    .AddCheck<ExternalApiHealthCheck>("external-api");

var app = builder.Build();

// ════════════════════════════════════════════════════════════════════════════
// Middleware Pipeline — ORDER MATTERS
// ════════════════════════════════════════════════════════════════════════════

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(o =>
    {
        o.Title = "My API";
        o.Theme = ScalarTheme.Purple;
        o.DefaultHttpClient = (ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseExceptionHandler();       // Must be first — catches exceptions from everything below
app.UseHttpsRedirection();
app.UseCors(app.Environment.IsDevelopment() ? "ViteDev" : "Production");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

// ── Map Endpoints ─────────────────────────────────────────────────────────────
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Map endpoint groups (see endpoint-groups.cs)
app.MapProductEndpoints();
app.MapOrderEndpoints();
app.MapAuthEndpoints();

app.Run();

// Make Program accessible for WebApplicationFactory in tests
public partial class Program { }
