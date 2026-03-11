# Security & Authentication — ASP.NET Core

Covers JWT bearer tokens, OAuth2/OIDC, ASP.NET Core Identity, authorization policies, and the Data Protection API.

---

## JWT Bearer Authentication

```csharp
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration
            .GetSection("JwtSettings").Get<JwtSettings>()!;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwt.Issuer,
            ValidAudience            = jwt.Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwt.Secret)),
            ClockSkew                = TimeSpan.FromSeconds(30)
        };

        // For SignalR / Blazor Server — read token from query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });
```

---

## Token Generation

```csharp
public class JwtTokenService(IOptions<JwtSettings> options)
{
    private readonly JwtSettings _jwt = options.Value;

    public string GenerateAccessToken(string userId, string email, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email,          email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key       = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds     = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer:             _jwt.Issuer,
            audience:           _jwt.Audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            expiresAt,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Refresh token — opaque, stored in DB or distributed cache
    public static string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
```

---

## Authorization Policies

```csharp
builder.Services.AddAuthorization(options =>
{
    // Role-based
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));

    // Claim-based
    options.AddPolicy("CanManageOrders", p =>
        p.RequireAuthenticatedUser()
         .RequireClaim("permissions", "orders:write"));

    // Age-based custom requirement
    options.AddPolicy("Over18", p =>
        p.Requirements.Add(new MinimumAgeRequirement(18)));

    // Combine multiple requirements
    options.AddPolicy("SeniorEditor", p =>
        p
        .RequireRole("Editor")
        .RequireClaim("tenure_years")
        .RequireAssertion(ctx =>
            int.TryParse(
                ctx.User.FindFirstValue("tenure_years"), out var years)
            && years >= 5));

    // Default policy — all authenticated
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Custom requirement handler
public class MinimumAgeRequirement(int minimumAge) : IAuthorizationRequirement { }

public class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx, MinimumAgeRequirement req)
    {
        var dob = ctx.User.FindFirstValue("date_of_birth");

        if (DateTime.TryParse(dob, out var birthDate) &&
            DateTime.Today.Year - birthDate.Year >= req.minimumAge)
            ctx.Succeed(req);

        return Task.CompletedTask;
    }
}
```

---

## ASP.NET Core Identity Setup

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

```csharp
// IdentityUser can be extended
public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName  { get; set; }
}

// DbContext
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options) { }

// Registration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength    = 8;
        options.Password.RequireDigit      = true;
        options.Password.RequireUppercase  = true;
        options.User.RequireUniqueEmail    = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
```

---

## OAuth2 / OIDC (External Identity Provider)

```csharp
// Entra ID (formerly Azure AD), Google, etc.
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Or generic OIDC
builder.Services.AddAuthentication()
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority    = "https://login.example.com";
        options.ClientId     = "my-api-client";
        options.ClientSecret = "...";  // Use secret manager
        options.ResponseType = "code";
        options.Scope.Add("profile");
        options.SaveTokens = true;
    });
```

---

## Applying Auth to Endpoints

```csharp
// Require authenticated user
group.MapGet("/", GetAll).RequireAuthorization();

// Require specific policy
group.MapDelete("/{id:int}", Delete).RequireAuthorization("AdminOnly");

// Require role
group.MapPost("/", Create).RequireAuthorization(p => p.RequireRole("Editor"));

// Allow anonymous in an otherwise authenticated group
group.MapGet("/public", GetPublic).AllowAnonymous();
```

---

## Learn More

| Topic | Query |
|-------|-------|
| JWT middleware | `microsoft_docs_search(query="ASP.NET Core JWT bearer authentication .NET 8")` |
| Identity | `microsoft_docs_search(query="ASP.NET Core Identity setup EntityFrameworkCore")` |
| Authorization policies | `microsoft_docs_search(query="ASP.NET Core authorization policies IAuthorizationRequirement")` |
| Entra ID / MSAL | `microsoft_docs_search(query="Microsoft.Identity.Web Entra ID ASP.NET Core")` |
| Data Protection API | `microsoft_docs_search(query="ASP.NET Core Data Protection IDataProtector")` |
