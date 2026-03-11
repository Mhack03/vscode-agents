# Authentication Patterns

## JWT (JSON Web Token) Authentication

### Generate Token

```javascript
const jwt = require("jsonwebtoken");

function generateToken(user, expiresIn = "1h") {
	return jwt.sign(
		{
			userId: user.id,
			email: user.email,
			role: user.role,
		},
		process.env.JWT_SECRET,
		{ expiresIn }
	);
}

// Refresh token strategy
const accessToken = generateToken(user, "15m");
const refreshToken = generateToken(user, "7d");

app.post("/auth/token", (req, res) => {
	const { refreshToken } = req.body;
	try {
		const decoded = jwt.verify(refreshToken, process.env.JWT_REFRESH_SECRET);
		const newAccessToken = generateToken(decoded, "15m");
		res.json({ accessToken: newAccessToken });
	} catch (err) {
		res.status(401).json({ error: "Invalid refresh token" });
	}
});
```

### Authentication Middleware

```javascript
async function authenticate(req, res, next) {
	try {
		const authHeader = req.header("Authorization");
		if (!authHeader?.startsWith("Bearer ")) {
			return res.status(401).json({
				error: { code: "NO_TOKEN", message: "Token required" },
			});
		}

		const token = authHeader.substring(7);
		const decoded = jwt.verify(token, process.env.JWT_SECRET);

		// Optional: Fetch user from DB to verify still active
		const user = await User.findById(decoded.userId);
		if (!user || !user.isActive) {
			return res.status(401).json({
				error: { code: "INVALID_USER", message: "User not found or inactive" },
			});
		}

		req.user = user;
		next();
	} catch (error) {
		if (error.name === "TokenExpiredError") {
			return res.status(401).json({
				error: {
					code: "TOKEN_EXPIRED",
					message: "Token expired, please refresh",
				},
			});
		}
		return res.status(401).json({
			error: { code: "INVALID_TOKEN", message: "Invalid token" },
		});
	}
}

app.use("/api", authenticate);
```

### Authorization Middleware

```javascript
function authorize(...allowedRoles) {
	return (req, res, next) => {
		if (!allowedRoles.includes(req.user.role)) {
			return res.status(403).json({
				error: { code: "FORBIDDEN", message: "Insufficient permissions" },
			});
		}
		next();
	};
}

// Usage
app.delete("/api/users/:id", authenticate, authorize("admin"), controller);
```

## ASP.NET Core JWT Setup

### Configure in Startup

```csharp
public void ConfigureServices(IServiceCollection services)
{
    var jwtSettings = Configuration.GetSection("JwtSettings");
    var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]);

    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

    services.AddAuthorization();
}

public void Configure(IApplicationBuilder app)
{
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseEndpoints(endpoints => endpoints.MapControllers());
}
```

### Generate Token

```csharp
public class AuthService
{
    private readonly IConfiguration _config;

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["JwtSettings:Secret"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"],
            audience: _config["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### Authorize in Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        // User must be authenticated
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Ok(new { userId });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        // Only admins can delete
        return Ok();
    }
}
```

## OAuth 2.0 / OIDC Integration

### With Microsoft Identity

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddMicrosoftIdentityWebApiAuthentication(Configuration);
}
```

### With Third-Party Provider

```javascript
const passport = require("passport");
const GoogleStrategy = require("passport-google-oauth20").Strategy;

passport.use(
	new GoogleStrategy(
		{
			clientID: process.env.GOOGLE_CLIENT_ID,
			clientSecret: process.env.GOOGLE_CLIENT_SECRET,
			callbackURL: "/auth/google/callback",
		},
		async (accessToken, refreshToken, profile, done) => {
			let user = await User.findOne({ googleId: profile.id });
			if (!user) {
				user = await User.create({
					googleId: profile.id,
					email: profile.emails[0].value,
					name: profile.displayName,
				});
			}
			return done(null, user);
		}
	)
);

app.get(
	"/auth/google",
	passport.authenticate("google", { scope: ["profile", "email"] })
);

app.get(
	"/auth/google/callback",
	passport.authenticate("google", { failureRedirect: "/login" }),
	(req, res) => {
		const token = generateToken(req.user);
		res.redirect(`/?token=${token}`);
	}
);
```

## API Key Authentication

### Middleware

```javascript
async function validateAPIKey(req, res, next) {
	const apiKey = req.header("X-API-Key");

	if (!apiKey) {
		return res.status(401).json({
			error: { code: "NO_API_KEY", message: "API key required" },
		});
	}

	// Hash the key before storing/comparing
	const hashedKey = crypto.createHash("sha256").update(apiKey).digest("hex");
	const client = await APIKey.findOne({
		keyHash: hashedKey,
		isActive: true,
	});

	if (!client) {
		// Log this - might indicate security issue
		logger.warn("Invalid API key attempt", { key: apiKey.substring(0, 5) });
		return res.status(401).json({
			error: { code: "INVALID_API_KEY", message: "Invalid API key" },
		});
	}

	// Update usage tracking
	await client.updateOne({ $inc: { requestCount: 1 } });

	req.client = client;
	next();
}

app.use("/api/external", validateAPIKey);
```

### Generate API Key

```javascript
const generateAPIKey = () => crypto.randomBytes(32).toString("hex");

app.post("/api-keys", authenticate, authorize("admin"), async (req, res) => {
	const apiKey = generateAPIKey();
	const hashedKey = crypto.createHash("sha256").update(apiKey).digest("hex");

	const record = await APIKey.create({
		userId: req.user.id,
		keyHash: hashedKey,
		name: req.body.name,
		isActive: true,
	});

	res.json({
		data: {
			id: record.id,
			name: record.name,
			key: apiKey, // Return once, only the hash is stored
		},
	});
});
```

## Token Rotation Strategy

```javascript
const tokenRotationMiddleware = async (req, res, next) => {
	const authHeader = req.header("Authorization");
	if (!authHeader) return next();

	const token = authHeader.substring(7);
	const decoded = jwt.decode(token);

	// Rotate token if expiring soon (within 1 minute)
	const expiresIn = decoded.exp - Math.floor(Date.now() / 1000);
	if (expiresIn < 60) {
		const newToken = generateToken(decoded);
		res.set("X-New-Token", newToken);
	}

	next();
};
```

## Best Practices

1. **Never store passwords** - Use bcrypt/Argon2
2. **Use HTTPS** - Always for auth endpoints
3. **Implement token rotation** - Refresh tokens separately
4. **Set appropriate expiration** - Short for access, longer for refresh
5. **Validate token signature** - Essential for security
6. **Store securely** - HttpOnly, Secure cookies or localStorage + XSS protection
7. **Implement logout** - Token blacklisting or expiration
8. **Monitor failed auth** - Log and alert on suspicious activity
9. **Use scopes** - Fine-grained permission control
10. **Implement CSRF** - For cookie-based auth
