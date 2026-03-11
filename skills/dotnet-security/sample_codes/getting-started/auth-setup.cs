// auth-setup.cs — Full JWT + Identity authentication setup
// Drop into Program.cs or call AddAuth() from here

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ─── Identity ───────────────────────────────────────────────────────────────
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ─── JWT Bearer ──────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
builder.Services.AddSingleton(jwtSettings);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        // Support tokens from cookies (for web clients)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Cookies["access_token"];
                if (token is not null) ctx.Token = token;
                return Task.CompletedTask;
            },
        };
    });

// ─── Authorization Policies ──────────────────────────────────────────────────
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("admin", p => p.RequireRole("Admin"))
    .AddPolicy("manager", p => p.RequireRole("Admin", "Manager"))
    .AddPolicy("verified", p => p.RequireClaim("email_verified", "true"))
    .AddPolicy("own-resource", p => p.RequireAssertion(ctx =>
    {
        var routeId = ctx.Resource is HttpContext httpCtx
            ? httpCtx.Request.RouteValues["id"]?.ToString()
            : null;
        return ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) == routeId
            || ctx.User.IsInRole("Admin");
    }));

// ─── Auth Services ───────────────────────────────────────────────────────────
builder.Services.AddScoped<AuthTokenService>();

var app = builder.Build();

// ─── Security Middleware ─────────────────────────────────────────────────────
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers.Remove("Server");
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// ─── Auth Endpoints ──────────────────────────────────────────────────────────
var auth = app.MapGroup("/api/auth").WithTags("Auth");

auth.MapPost("/login", async (
    LoginRequest req,
    UserManager<ApplicationUser> users,
    AuthTokenService tokens,
    HttpContext ctx,
    CancellationToken ct) =>
{
    var user = await users.FindByEmailAsync(req.Email);
    if (user is null || !await users.CheckPasswordAsync(user, req.Password))
        return TypedResults.Problem("Invalid email or password.", statusCode: 401);

    if (await users.IsLockedOutAsync(user))
        return TypedResults.Problem("Account locked. Try again later.", statusCode: 423);

    var roles = await users.GetRolesAsync(user);
    var ip = ctx.Connection.RemoteIpAddress?.ToString();
    var pair = await tokens.IssueTokensAsync(user, [.. roles], ip, ct);

    ctx.Response.Cookies.Append("refresh_token", pair.RefreshToken,
        new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

    return TypedResults.Ok(new { accessToken = pair.AccessToken });
})
.WithSummary("Login with email and password")
.AllowAnonymous();

auth.MapPost("/refresh", async (
    HttpContext ctx,
    AuthTokenService tokens,
    CancellationToken ct) =>
{
    var rawToken = ctx.Request.Cookies["refresh_token"];
    if (rawToken is null) return TypedResults.Unauthorized();

    var ip = ctx.Connection.RemoteIpAddress?.ToString();
    var pair = await tokens.RotateRefreshTokenAsync(rawToken, ip, ct);
    if (pair is null) return TypedResults.Unauthorized();

    ctx.Response.Cookies.Append("refresh_token", pair.RefreshToken,
        new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

    return TypedResults.Ok(new { accessToken = pair.AccessToken });
})
.AllowAnonymous();

auth.MapPost("/logout", async (
    HttpContext ctx,
    AuthTokenService tokens,
    CancellationToken ct) =>
{
    var rawToken = ctx.Request.Cookies["refresh_token"];
    if (rawToken is not null)
        await tokens.RevokeRefreshTokenAsync(rawToken, ct);

    ctx.Response.Cookies.Delete("refresh_token");
    return TypedResults.NoContent();
})
.RequireAuthorization();

// ─── DTOs ─────────────────────────────────────────────────────────────────────
public record LoginRequest(string Email, string Password);

public class JwtSettings
{
    public string SecretKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int AccessExpiryMinutes { get; init; } = 15;
    public int RefreshExpiryDays { get; init; } = 7;
}
