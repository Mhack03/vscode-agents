// token-service.cs — JWT generation + refresh token rotation
// Demonstrates: JwtTokenService, refresh token store, token pair

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

public class AuthTokenService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IOptions<JwtSettings> jwtOptions,
    ILogger<AuthTokenService> logger)
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    // ─── Issue new access + refresh token pair ────────────────────────────────
    public async Task<TokenPair> IssueTokensAsync(
        ApplicationUser user,
        IList<string> roles,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var accessToken = GenerateAccessToken(user, roles);
        var rawRefresh = GenerateRawRefreshToken();
        var hash = HashToken(rawRefresh);

        // Revoke all existing active tokens for this user (single active session)
        await db.RefreshTokens
            .Where(t => t.UserId == user.Id && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsRevoked, true)
                .SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshExpiryDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
        });

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Issued token pair for user {UserId}", user.Id);
        return new TokenPair(accessToken, rawRefresh);
    }

    // ─── Rotate refresh token ─────────────────────────────────────────────────
    public async Task<TokenPair?> RotateRefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var hash = HashToken(rawRefreshToken);
        var token = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token is null)
        {
            logger.LogWarning("Refresh token not found — possible theft or reuse");
            return null;
        }

        if (!token.IsActive)
        {
            // Token reuse detected — revoke entire family
            if (token.IsRevoked)
            {
                logger.LogWarning("Refresh token reuse detected for user {UserId}", token.UserId);
                await RevokeAllUserTokensAsync(token.UserId, ct);
            }
            return null;
        }

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;

        var roles = await userManager.GetRolesAsync(token.User);
        var pair = await IssueTokensAsync(token.User, roles, ipAddress, ct);
        return pair;
    }

    // ─── Revoke by raw token ──────────────────────────────────────────────────
    public async Task RevokeRefreshTokenAsync(string rawToken, CancellationToken ct)
    {
        await db.RefreshTokens
            .Where(t => t.TokenHash == HashToken(rawToken))
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsRevoked, true)
                .SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
    }

    // ─── Revoke all tokens for a user (security event) ───────────────────────
    private async Task RevokeAllUserTokensAsync(string userId, CancellationToken ct)
    {
        await db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsRevoked, true)
                .SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
    }

    // ─── JWT Generation ───────────────────────────────────────────────────────
    private string GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new("name", $"{user.FirstName} {user.LastName}"),
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_jwt.AccessExpiryMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRawRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public record TokenPair(string AccessToken, string RefreshToken);
