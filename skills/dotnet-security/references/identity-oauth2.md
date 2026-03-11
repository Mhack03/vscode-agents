# ASP.NET Core Identity & OAuth2 / OpenID Connect

---

## Identity Setup

```csharp
// Program.cs
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password
        options.Password.RequiredLength         = 12;
        options.Password.RequireDigit           = true;
        options.Password.RequireLowercase       = true;
        options.Password.RequireUppercase       = true;
        options.Password.RequireNonAlphanumeric = true;

        // Lockout
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers      = true;

        // User
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;

        // Tokens
        options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
```

---

## Custom ApplicationUser

```csharp
public class ApplicationUser : IdentityUser
{
    public string        FirstName   { get; set; } = string.Empty;
    public string        LastName    { get; set; } = string.Empty;
    public DateTime?     DateOfBirth { get; set; }
    public bool          IsActive    { get; set; } = true;
    public string?       AvatarUrl   { get; set; }
    public DateTime      CreatedAt   { get; set; } = DateTime.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
```

---

## Seed Roles & Admin User

```csharp
public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Seed roles
        string[] roles = ["Admin", "Manager", "Customer"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Seed admin user
        const string adminEmail = "admin@example.com";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName  = adminEmail,
                Email     = adminEmail,
                FirstName = "System",
                LastName  = "Admin",
                EmailConfirmed = true,
            };
            var result = await userManager.CreateAsync(admin, "Admin@12345678");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}

// In Program.cs
await DatabaseSeeder.SeedAsync(app.Services);
```

---

## Microsoft Entra ID (Azure AD) Login

```bash
dotnet add package Microsoft.Identity.Web
dotnet add package Microsoft.Identity.Web.MicrosoftGraph
```

```json
// appsettings.json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-secret",
    "CallbackPath": "/signin-oidc"
  }
}
```

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
```

---

## Google OAuth2

```bash
dotnet add package Microsoft.AspNetCore.Authentication.Google
```

```csharp
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme          = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddGoogle(options =>
    {
        options.ClientId     = builder.Configuration["Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Google:ClientSecret"]!;
        options.CallbackPath = "/signin-google";
        options.Scope.Add("email");
        options.Scope.Add("profile");

        options.Events.OnCreatingTicket = async ctx =>
        {
            // Provision user on first login
            var userManager = ctx.HttpContext.RequestServices
                .GetRequiredService<UserManager<ApplicationUser>>();
            var email = ctx.Principal?.FindFirstValue(ClaimTypes.Email);
            if (email is not null && await userManager.FindByEmailAsync(email) is null)
            {
                var user = new ApplicationUser
                {
                    Email          = email,
                    UserName       = email,
                    EmailConfirmed = true,
                };
                await userManager.CreateAsync(user);
                await userManager.AddToRoleAsync(user, "Customer");
            }
        };
    });
```

---

## JWT Claims from Identity

```csharp
private string GenerateAccessToken(ApplicationUser user, IList<string> roles)
{
    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id),
        new(JwtRegisteredClaimNames.Email, user.Email!),
        new(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()),
        new("name", $"{user.FirstName} {user.LastName}"),
    };

    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

    var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));
    var token = new JwtSecurityToken(
        issuer:   _jwt.Issuer,
        audience: _jwt.Audience,
        claims:   claims,
        expires:  DateTime.UtcNow.AddMinutes(_jwt.AccessExpiryMinutes),
        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

---

## Learn More

| Topic | Query |
|-------|-------|
| ASP.NET Core Identity | `microsoft_docs_search(query="ASP.NET Core Identity custom user roles claims configuration")` |
| Microsoft Entra ID | `microsoft_docs_search(query="Microsoft.Identity.Web Azure AD JWT bearer ASP.NET Core")` |
| OAuth2 external login | `microsoft_docs_search(query="ASP.NET Core external OAuth login Google Facebook callback")` |
