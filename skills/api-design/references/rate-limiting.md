# Rate Limiting & Circuit Breaker Patterns

## Express.js Rate Limiting

### Basic Rate Limiting

```javascript
const rateLimit = require("express-rate-limit");
const RedisStore = require("rate-limit-redis");
const redis = require("redis").createClient();

const limiter = rateLimit({
	store: new RedisStore({
		client: redis,
		prefix: "rl:",
	}),
	windowMs: 15 * 60 * 1000, // 15 minutes
	max: 100, // Limit each IP to 100 requests per windowMs
	message: {
		error: {
			code: "RATE_LIMIT_EXCEEDED",
			message: "Too many requests, please try again later",
		},
	},
	standardHeaders: true, // Return rate limit info in headers
	legacyHeaders: false,
	skip: (req) => req.user?.role === "admin", // Exempt admins
	keyGenerator: (req) => req.ip,
});

// Apply to all API routes
app.use("/api/", limiter);
```

### Per-User Rate Limiting

```javascript
const userLimiter = rateLimit({
	store: new RedisStore({
		client: redis,
		prefix: "user-rl:",
	}),
	windowMs: 60 * 1000, // 1 minute
	max: 60, // 60 requests per minute per user
	keyGenerator: (req) => req.user?.id || req.ip,
	skipSuccessfulRequests: false,
	skipFailedRequests: true, // Don't count failed requests
});

app.use("/api/", authenticate, userLimiter);
```

### Different Limits by Endpoint

```javascript
const strictLimiter = rateLimit({
	windowMs: 60 * 1000,
	max: 3, // Very strict
	keyGenerator: (req) => req.ip,
});

const standardLimiter = rateLimit({
	windowMs: 15 * 60 * 1000,
	max: 100,
	keyGenerator: (req) => req.ip,
});

// Strict limits on auth endpoints
app.post("/auth/login", strictLimiter, loginController);
app.post("/auth/register", strictLimiter, registerController);
app.post("/password-reset", strictLimiter, resetController);

// Standard limits on regular endpoints
app.get("/api/users", standardLimiter, getUsersController);
```

### Tiered Rate Limiting

```javascript
const getRateLimit = (req) => {
	const plan = req.user?.plan || "free";

	const limits = {
		free: { windowMs: 60 * 1000, max: 10 },
		pro: { windowMs: 60 * 1000, max: 100 },
		enterprise: { windowMs: 60 * 1000, max: -1 }, // Unlimited
	};

	return limits[plan] || limits.free;
};

const tieredLimiter = rateLimit({
	store: new RedisStore({ client: redis, prefix: "tiered-rl:" }),
	windowMs: (req) => getRateLimit(req).windowMs,
	max: (req) => getRateLimit(req).max,
	keyGenerator: (req) => req.user?.id || req.ip,
});

app.use("/api/", authenticate, tieredLimiter);
```

## Circuit Breaker Pattern

### Basic Implementation

```javascript
class CircuitBreaker {
	constructor(fn, options = {}) {
		this.fn = fn;
		this.state = "CLOSED"; // CLOSED, OPEN, HALF_OPEN
		this.failureCount = 0;
		this.threshold = options.threshold || 5;
		this.timeout = options.timeout || 60000;
		this.nextAttempt = Date.now();
	}

	async execute(...args) {
		if (this.state === "OPEN") {
			if (Date.now() < this.nextAttempt) {
				throw new Error("Circuit breaker is OPEN");
			}
			this.state = "HALF_OPEN";
		}

		try {
			const result = await this.fn(...args);
			this.onSuccess();
			return result;
		} catch (error) {
			this.onFailure();
			throw error;
		}
	}

	onSuccess() {
		this.failureCount = 0;
		this.state = "CLOSED";
	}

	onFailure() {
		this.failureCount++;
		if (this.failureCount >= this.threshold) {
			this.state = "OPEN";
			this.nextAttempt = Date.now() + this.timeout;
		}
	}

	getState() {
		return this.state;
	}
}

// Usage
const externalAPI = new CircuitBreaker(
	async (url) => {
		const res = await fetch(url, { timeout: 10000 });
		if (!res.ok) throw new Error(`HTTP ${res.status}`);
		return res.json();
	},
	{ threshold: 5, timeout: 60000 }
);

app.get("/api/data", async (req, res) => {
	try {
		const data = await externalAPI.execute("https://external-api.com/data");
		res.json({ data });
	} catch (error) {
		if (error.message.includes("Circuit breaker")) {
			return res.status(503).json({
				error: {
					code: "SERVICE_UNAVAILABLE",
					message: "Service temporarily unavailable",
				},
			});
		}
		res.status(502).json({
			error: { code: "UPSTREAM_ERROR", message: error.message },
		});
	}
});
```

### Circuit Breaker with Fallback

```javascript
class CircuitBreakerWithFallback extends CircuitBreaker {
	constructor(fn, fallback, options) {
		super(fn, options);
		this.fallback = fallback;
	}

	async execute(...args) {
		try {
			return await super.execute(...args);
		} catch (error) {
			if (this.fallback) {
				logger.warn("Circuit breaker fallback activated", {
					error: error.message,
				});
				return await this.fallback(...args);
			}
			throw error;
		}
	}
}

// Usage
const apiWithFallback = new CircuitBreakerWithFallback(
	(id) => fetchFromAPI(id),
	(id) => getCachedData(id), // Fallback to cache
	{ threshold: 5, timeout: 60000 }
);
```

## ASP.NET Core Rate Limiting

### Using Built-in Rate Limiting

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User?.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(15)
                }));
    });

    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();
}

public void Configure(IApplicationBuilder app)
{
    app.UseRateLimiter();
}
```

### Custom Rate Limiting Policy

```csharp
[Authorize]
[HttpGet("api/users")]
[RateLimitPolicy("strict")]
public async Task<IActionResult> GetUsers()
{
    return Ok();
}

// In Startup
services.AddRateLimiter(options =>
{
    options.AddPolicy("strict", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.FindFirst("sub")?.Value ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddPolicy("standard", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(15)
            }));
});
```

## Retry Strategy with Exponential Backoff

```javascript
async function retryWithBackoff(fn, maxRetries = 3, initialDelay = 100) {
	for (let attempt = 0; attempt <= maxRetries; attempt++) {
		try {
			return await fn();
		} catch (error) {
			if (attempt === maxRetries) {
				throw error;
			}

			// Only retry on specific errors
			if (![408, 429, 500, 502, 503, 504].includes(error.statusCode)) {
				throw error;
			}

			// Exponential backoff with jitter
			const delay = initialDelay * Math.pow(2, attempt);
			const jitter = Math.random() * delay * 0.1;
			await sleep(delay + jitter);
		}
	}
}

// Usage
const data = await retryWithBackoff(
	() => fetchExternalAPI("https://api.example.com/data"),
	3,
	100
);
```

## Monitoring Rate Limits

```javascript
const monitorRateLimits = (req, res, next) => {
	res.on("finish", () => {
		const remaining = res.get("RateLimit-Remaining");
		const limit = res.get("RateLimit-Limit");
		const reset = res.get("RateLimit-Reset");

		// Alert when limit is approaching
		if (remaining && limit && parseInt(remaining) / parseInt(limit) < 0.1) {
			logger.warn("Rate limit approaching", {
				user: req.user?.id,
				remaining,
				limit,
				reset,
			});
		}
	});

	next();
};

app.use(monitorRateLimits);
```

## Best Practices

1. **Use per-user limits** - Not just per-IP
2. **Implement tiered pricing** - Different limits for different plans
3. **Provide clear headers** - RateLimit-Limit, RateLimit-Remaining, RateLimit-Reset
4. **Use Redis for distribution** - For multi-server setups
5. **Implement circuit breaker** - For external API integration
6. **Add retry logic** - With exponential backoff
7. **Monitor breakers** - Alert when circuit opens
8. **Exempt critical endpoints** - Admin operations might bypass limits
9. **Graceful degradation** - Return useful error messages
10. **Document limits** - Include in API documentation
