# JWT Refresh Token Rotation

---

## Refresh Token Flow

```
1. POST /api/auth/login     → returns { accessToken, refreshToken }
2. Access token expires (15–60 min)
3. POST /api/auth/refresh   → { refreshToken } → returns new { accessToken, refreshToken }
4. Old refresh token is REVOKED immediately (rotation)
5. POST /api/auth/logout    → revokes current refresh token
```

---

## RefreshToken Entity

```csharp
public class RefreshToken
{
    public int      Id          { get; set; }
    public string   UserId      { get; set; } = string.Empty;
    public string   TokenHash   { get; set; } = string.Empty;  // SHA256 of raw token
    public DateTime ExpiresAt   { get; set; }
    public DateTime CreatedAt   { get; set; }
    public string?  CreatedByIp { get; set; }
    public bool     IsRevoked   { get; set; }
    public DateTime? RevokedAt  { get; set; }

    public bool IsExpired  => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive   => !IsRevoked && !IsExpired;
}
```

---

## Token Service

```csharp
public class AuthTokenService(
    AppDbContext context,
    IOptions<JwtSettings> jwtOptions)
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    public async Task<TokenPair> IssueTokensAsync(
        ApplicationUser user,
        IList<string> roles,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var accessToken  = GenerateAccessToken(user, roles);
        var rawRefresh   = GenerateRawRefreshToken();
        var refreshHash  = HashToken(rawRefresh);

        // Revoke all active tokens for this user (single active session)
        // Remove this block to allow multiple devices
        await context.RefreshTokens
            .Where(t => t.UserId == user.Id && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsRevoked,  true)
                .SetProperty(t => t.RevokedAt,  DateTime.UtcNow), ct);

        context.RefreshTokens.Add(new RefreshToken
        {
            UserId      = user.Id,
            TokenHash   = refreshHash,
            ExpiresAt   = DateTime.UtcNow.AddDays(_jwt.RefreshExpiryDays),
            CreatedAt   = DateTime.UtcNow,
            CreatedByIp = ipAddress,
        });

        await context.SaveChangesAsync(ct);
        return new TokenPair(accessToken, rawRefresh);
    }

    public async Task<TokenPair?> RotateRefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var hash  = HashToken(rawRefreshToken);
        var token = await context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token is null || !token.IsActive)
            return null;  // Invalid, expired, or already rotated

        // Revoke old token
        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;

        // Issue new pair
        var roles = await userManager.GetRolesAsync(token.User);
        var pair  = await IssueTokensAsync(token.User, roles, ipAddress, ct);
        return pair;
    }

    public async Task RevokeRefreshTokenAsync(string rawToken, CancellationToken ct)
    {
        var hash = HashToken(rawToken);
        await context.RefreshTokens
            .Where(t => t.TokenHash == hash)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsRevoked, true)
                .SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
    }

    private static string GenerateRawRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}

public record TokenPair(string AccessToken, string RefreshToken);
```

---

## Auth Endpoints

```csharp
auth.MapPost("/login", async (LoginRequest req, AuthTokenService tokens,
    UserManager<ApplicationUser> users, HttpContext ctx, CancellationToken ct) =>
{
    var user = await users.FindByEmailAsync(req.Email);
    if (user is null || !await users.CheckPasswordAsync(user, req.Password))
        return TypedResults.Unauthorized();

    if (await users.IsLockedOutAsync(user))
        return TypedResults.Problem("Account locked.", statusCode: 423);

    var roles = await users.GetRolesAsync(user);
    var ip    = ctx.Connection.RemoteIpAddress?.ToString();
    var pair  = await tokens.IssueTokensAsync(user, roles, ip, ct);

    // Refresh token in HttpOnly cookie — prevents JS access
    ctx.Response.Cookies.Append("refresh_token", pair.RefreshToken,
        new CookieOptions
        {
            HttpOnly  = true,
            Secure    = true,
            SameSite  = SameSiteMode.Strict,
            Expires   = DateTimeOffset.UtcNow.AddDays(7),
        });

    return TypedResults.Ok(new { accessToken = pair.AccessToken });
});

auth.MapPost("/refresh", async (HttpContext ctx, AuthTokenService tokens, CancellationToken ct) =>
{
    var rawToken = ctx.Request.Cookies["refresh_token"];
    if (rawToken is null) return TypedResults.Unauthorized();

    var ip     = ctx.Connection.RemoteIpAddress?.ToString();
    var pair   = await tokens.RotateRefreshTokenAsync(rawToken, ip, ct);
    if (pair is null) return TypedResults.Unauthorized();

    ctx.Response.Cookies.Append("refresh_token", pair.RefreshToken, /* same opts */ );
    return TypedResults.Ok(new { accessToken = pair.AccessToken });
});
```

---

## Sliding Expiry (optional)

```csharp
// Increase token lifetime if used recently (within half its lifetime)
if (token.ExpiresAt - DateTime.UtcNow < TimeSpan.FromDays(_jwt.RefreshExpiryDays / 2))
{
    token.ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshExpiryDays);
    await context.SaveChangesAsync(ct);
}
```

---

## Learn More

| Topic | Query |
|-------|-------|
| JWT refresh tokens | `microsoft_docs_search(query="ASP.NET Core JWT refresh token rotation revocation")` |
| HttpOnly cookies | `microsoft_docs_search(query="ASP.NET Core cookie HttpOnly Secure SameSite options")` |
