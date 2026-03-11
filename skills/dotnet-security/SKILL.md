---
name: dotnet-security
description: JWT authentication, ASP.NET Core Identity, OAuth2/OIDC, authorization policies, secret management, and OWASP protections for .NET 8/9/10 Web APIs.
license: Complete terms in LICENSE.txt
---

# .NET Security Patterns

## When to Use This Skill

- Setting up JWT bearer authentication in a .NET Web API
- Configuring ASP.NET Core Identity with EF Core
- Integrating OAuth2/OIDC (Entra ID, Google, Auth0)
- Implementing role-based or policy-based authorization
- Managing secrets (User Secrets, Azure Key Vault)
- Protecting against OWASP API Security Top 10
- Generating and validating refresh tokens

---

## JWT Bearer Authentication — Minimal Setup

```csharp
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        var s = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = s.Issuer,
            ValidAudience            = s.Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(s.Secret)),
            ClockSkew                = TimeSpan.FromSeconds(30),
        };
    });

app.UseAuthentication();
app.UseAuthorization();
```

---

## Token Generation

```csharp
public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id),
        new(ClaimTypes.Email,          user.Email!),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    };
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

    var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
    var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token  = new JwtSecurityToken(
        issuer:             _settings.Issuer,
        audience:           _settings.Audience,
        claims:             claims,
        expires:            DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
        signingCredentials: creds);

    return new JwtSecurityTokenHandler().WriteToken(token);
}

// Opaque refresh token — store hash in DB, never plain text
public static string GenerateRefreshToken() =>
    Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
```

---

## Authorization Policies

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",        p => p.RequireRole("Admin"));
    options.AddPolicy("CanWriteProducts", p =>
        p.RequireAuthenticatedUser()
         .RequireClaim("permissions", "products:write"));
});

// Apply to endpoint
group.MapDelete("/{id:int}", Delete)
     .RequireAuthorization("AdminOnly");

// Allow anonymous in otherwise protected group
group.MapGet("/public", GetPublic).AllowAnonymous();
```

---

## ASP.NET Core Identity — Minimal Registration

```csharp
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(o =>
    {
        o.Password.RequiredLength           = 12;
        o.Password.RequireDigit             = true;
        o.Password.RequireUppercase         = true;
        o.Password.RequireNonAlphanumeric   = true;
        o.User.RequireUniqueEmail           = true;
        o.Lockout.MaxFailedAccessAttempts   = 5;
        o.Lockout.DefaultLockoutTimeSpan    = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
```

---

## Secret Management

```bash
# Development — User Secrets (never commit appsettings with secrets)
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "dev-only-32-char-secret-here-x!"

# Production — Environment variables (preferred for containers)
# JWT__SECRET=... in Docker/K8s config

# Azure Key Vault (enterprise)
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
```

```csharp
// Azure Key Vault integration
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUri = builder.Configuration["KeyVault:Uri"]!;
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}
```

---

## OWASP API Security Checklist (.NET)

| Risk                      | .NET Mitigation                                                    |
| ------------------------- | ------------------------------------------------------------------ |
| Broken Object Auth        | Use `[Authorize]` + `ClaimTypes.NameIdentifier` checks             |
| Broken Auth               | JWT with short expiry + refresh token rotation                     |
| Excessive Data Exposure   | Never return entities — always project to DTOs                     |
| Rate Limiting             | `AddRateLimiter` in Program.cs                                     |
| Mass Assignment           | Use `[FromBody]` request records — never bind to entities          |
| Injection                 | EF Core parameterized queries; avoid `FromSqlRaw` with user input  |
| Security Misconfiguration | `UseHsts()`, `UseHttpsRedirection()`, remove dev endpoints in prod |
| Input Validation          | FluentValidation or DataAnnotations on every request type          |

---

## Security Headers Middleware

```csharp
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"]    = "nosniff";
    ctx.Response.Headers["X-Frame-Options"]           = "DENY";
    ctx.Response.Headers["X-XSS-Protection"]          = "1; mode=block";
    ctx.Response.Headers["Referrer-Policy"]           = "strict-origin-when-cross-origin";
    ctx.Response.Headers["Permissions-Policy"]        = "geolocation=(), microphone=()";
    await next();
});
```

---

## References

| Topic                                                    | Load When                                          |
| -------------------------------------------------------- | -------------------------------------------------- |
| [JWT & Refresh Tokens](references/jwt-refresh-tokens.md) | Refresh token rotation, revocation, sliding expiry |
| [Identity & OAuth2](references/identity-oauth2.md)       | ASP.NET Identity setup, Entra ID, Google, Auth0    |
| [OWASP Protections](references/owasp-protections.md)     | SQL injection, XSS, CSRF, input sanitization       |

## Learn More

| Topic                  | Query                                                                                                         |
| ---------------------- | ------------------------------------------------------------------------------------------------------------- |
| JWT bearer             | `microsoft_docs_fetch(url="https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn")` |
| ASP.NET Identity       | `microsoft_docs_search(query="ASP.NET Core Identity setup EntityFrameworkCore .NET 9")`                       |
| Authorization policies | `microsoft_docs_search(query="ASP.NET Core authorization policy IAuthorizationRequirement custom handler")`   |
| Azure Key Vault        | `microsoft_docs_search(query="Azure Key Vault ASP.NET Core configuration DefaultAzureCredential")`            |
