# External API Integration

## Retry Strategy with Exponential Backoff

### Basic Implementation

```javascript
async function fetchWithRetry(url, options = {}, maxRetries = 3) {
	const {
		timeout = 10000,
		retryableStatuses = [408, 429, 500, 502, 503, 504],
	} = options;

	for (let attempt = 0; attempt <= maxRetries; attempt++) {
		try {
			const controller = new AbortController();
			const timeoutId = setTimeout(() => controller.abort(), timeout);

			const response = await fetch(url, {
				...options,
				signal: controller.signal,
			});

			clearTimeout(timeoutId);

			if (!response.ok) {
				// Check if status is retryable
				if (!retryableStatuses.includes(response.status)) {
					throw new Error(`HTTP ${response.status}: ${response.statusText}`);
				}

				// Retryable error - back off
				if (attempt < maxRetries) {
					const delay = calculateBackoff(attempt);
					console.log(`Retry attempt ${attempt + 1} after ${delay}ms`);
					await sleep(delay);
					continue;
				}
			}

			return response;
		} catch (error) {
			if (attempt === maxRetries) {
				throw new Error(`Failed after ${maxRetries} retries: ${error.message}`);
			}

			const delay = calculateBackoff(attempt);
			console.log(`Retry attempt ${attempt + 1} after ${delay}ms`);
			await sleep(delay);
		}
	}
}

function calculateBackoff(attempt, baseDelay = 1000, maxDelay = 30000) {
	// Exponential backoff with jitter
	const exponentialDelay = baseDelay * Math.pow(2, attempt);
	const jitter = Math.random() * exponentialDelay * 0.1;
	return Math.min(exponentialDelay + jitter, maxDelay);
}

function sleep(ms) {
	return new Promise((resolve) => setTimeout(resolve, ms));
}

// Usage
const response = await fetchWithRetry(
	"https://api.example.com/data",
	{
		method: "GET",
		headers: { Authorization: "Bearer token" },
	},
	3
);

const data = await response.json();
```

## Circuit Breaker for External APIs

### Complete Implementation

```javascript
class CircuitBreaker {
	constructor(name, options = {}) {
		this.name = name;
		this.state = "CLOSED"; // CLOSED, OPEN, HALF_OPEN
		this.failureCount = 0;
		this.successCount = 0;
		this.lastFailureTime = null;

		this.failureThreshold = options.failureThreshold || 5;
		this.successThreshold = options.successThreshold || 2;
		this.timeout = options.timeout || 60000;
		this.onStateChange = options.onStateChange || (() => {});
	}

	async execute(fn, fallback = null) {
		if (this.state === "OPEN") {
			if (Date.now() - this.lastFailureTime < this.timeout) {
				// Still in timeout period
				if (fallback) {
					console.warn(`[${this.name}] Circuit OPEN, using fallback`);
					return await fallback();
				}
				throw new Error(`[${this.name}] Circuit breaker is OPEN`);
			}

			// Timeout expired, try to recover
			this.setState("HALF_OPEN");
		}

		try {
			const result = await fn();
			this.onSuccess();
			return result;
		} catch (error) {
			this.onFailure();

			if (fallback) {
				console.warn(
					`[${this.name}] Request failed, using fallback: ${error.message}`
				);
				return await fallback();
			}

			throw error;
		}
	}

	onSuccess() {
		this.failureCount = 0;

		if (this.state === "HALF_OPEN") {
			this.successCount++;
			if (this.successCount >= this.successThreshold) {
				this.setState("CLOSED");
				this.successCount = 0;
			}
		} else if (this.state === "CLOSED") {
			this.successCount = 0;
		}
	}

	onFailure() {
		this.lastFailureTime = Date.now();
		this.successCount = 0;
		this.failureCount++;

		if (this.failureCount >= this.failureThreshold) {
			this.setState("OPEN");
		}
	}

	setState(newState) {
		if (this.state !== newState) {
			console.log(`[${this.name}] State: ${this.state} -> ${newState}`);
			this.state = newState;
			this.onStateChange(newState);
		}
	}

	getMetrics() {
		return {
			name: this.name,
			state: this.state,
			failureCount: this.failureCount,
			successCount: this.successCount,
		};
	}
}

// Usage
const userServiceBreaker = new CircuitBreaker("UserService", {
	failureThreshold: 5,
	timeout: 60000,
	onStateChange: (newState) => {
		logger.warn(`User Service circuit breaker changed to ${newState}`);
	},
});

app.get("/api/users/:id", async (req, res) => {
	try {
		const user = await userServiceBreaker.execute(
			() => fetchFromExternalService(`/users/${req.params.id}`),
			() => getCachedUser(req.params.id) // Fallback
		);
		res.json({ data: user });
	} catch (error) {
		res.status(503).json({
			error: {
				code: "SERVICE_UNAVAILABLE",
				message: "Service temporarily unavailable",
			},
		});
	}
});
```

### Monitor Circuit Breakers

```javascript
const breakers = new Map();

function getOrCreateBreaker(name, options) {
	if (!breakers.has(name)) {
		breakers.set(name, new CircuitBreaker(name, options));
	}
	return breakers.get(name);
}

// Health check endpoint
app.get("/health/breakers", (req, res) => {
	const metrics = Array.from(breakers.values()).map((b) => b.getMetrics());
	const allHealthy = metrics.every((m) => m.state === "CLOSED");

	res.status(allHealthy ? 200 : 503).json({
		status: allHealthy ? "healthy" : "degraded",
		breakers: metrics,
	});
});
```

## HttpClient with Timeouts

### Node.js Implementation

```javascript
const http = require("http");
const https = require("https");

class HttpClient {
	constructor(options = {}) {
		this.timeout = options.timeout || 10000;
		this.retries = options.retries || 3;
	}

	async request(url, options = {}) {
		const mergedOptions = {
			timeout: this.timeout,
			...options,
		};

		return new Promise((resolve, reject) => {
			const isHttps = url.startsWith("https");
			const client = isHttps ? https : http;

			const req = client.request(url, mergedOptions, (res) => {
				let data = "";

				res.on("data", (chunk) => {
					data += chunk;
				});

				res.on("end", () => {
					if (res.statusCode >= 200 && res.statusCode < 300) {
						resolve({
							status: res.statusCode,
							headers: res.headers,
							body: data,
						});
					} else {
						reject(new Error(`HTTP ${res.statusCode}`));
					}
				});
			});

			req.on("timeout", () => {
				req.destroy();
				reject(new Error(`Request timeout after ${this.timeout}ms`));
			});

			req.on("error", reject);

			if (options.body) {
				req.write(options.body);
			}

			req.end();
		});
	}
}
```

### ASP.NET Core Implementation

```csharp
public class ResilientHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly int _timeoutMs;
    private readonly int _maxRetries;

    public ResilientHttpClient(int timeoutMs = 10000, int maxRetries = 3)
    {
        _timeoutMs = timeoutMs;
        _maxRetries = maxRetries;

        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
    }

    public async Task<T> GetAsync<T>(string url)
    {
        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                using var response = await _httpClient.GetAsync(url);

                if ((int)response.StatusCode >= 500 || response.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
                {
                    if (attempt < _maxRetries)
                    {
                        await Task.Delay(GetBackoffDelay(attempt));
                        continue;
                    }
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (HttpRequestException ex) when (attempt < _maxRetries)
            {
                await Task.Delay(GetBackoffDelay(attempt));
            }
        }

        throw new HttpRequestException($"Request failed after {_maxRetries} retries");
    }

    private int GetBackoffDelay(int attempt)
    {
        return (int)Math.Min(
            1000 * Math.Pow(2, attempt),
            30000 // Maximum 30 seconds
        );
    }
}

// Usage
services.AddScoped<ResilientHttpClient>();

[ApiController]
public class DataController : ControllerBase
{
    private readonly ResilientHttpClient _httpClient;

    [HttpGet("external-data")]
    public async Task<IActionResult> GetExternalData()
    {
        try
        {
            var data = await _httpClient.GetAsync<object>(
                "https://api.example.com/data");
            return Ok(data);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new { error = "Upstream service error" });
        }
    }
}
```

## Request Deduplication

```javascript
class RequestDeduplicator {
	constructor() {
		this.pending = new Map();
	}

	async execute(key, fn) {
		// Check if request is already in progress
		if (this.pending.has(key)) {
			return this.pending.get(key);
		}

		// Execute and deduplicate
		const promise = fn().finally(() => {
			this.pending.delete(key);
		});

		this.pending.set(key, promise);
		return promise;
	}
}

// Usage: Multiple requests for same user only make one API call
const deduplicator = new RequestDeduplicator();

app.get("/api/users/:id", async (req, res) => {
	try {
		const user = await deduplicator.execute(`user:${req.params.id}`, () =>
			fetchUserFromExternalService(req.params.id)
		);
		res.json({ data: user });
	} catch (error) {
		res.status(502).json({ error: error.message });
	}
});
```

## Webhook Handlers

```javascript
// When receiving webhooks from external service
app.post("/webhooks/payment", (req, res) => {
	const { event, data } = req.body;

	// Verify webhook signature
	const signature = req.header("X-Signature");
	if (!verifySignature(req.rawBody, signature)) {
		return res.status(401).json({ error: "Invalid signature" });
	}

	// Queue processing (don't hold up response)
	webhookQueue.push({ event, data, timestamp: Date.now() });

	// Return immediately
	res.json({ received: true });

	// Process asynchronously
	processWebhook(event, data).catch((error) => {
		logger.error("Webhook processing error", { event, error });
	});
});

// Process queued webhooks
async function processWebhook(event, data) {
	switch (event) {
		case "payment.success":
			await handlePaymentSuccess(data);
			break;
		case "payment.failed":
			await handlePaymentFailed(data);
			break;
	}
}
```

## Best Practices

1. **Set timeouts** - Always, default to 10 seconds
2. **Implement retries** - With exponential backoff
3. **Use circuit breakers** - For fault isolation
4. **Monitor metrics** - Track success/failure rates
5. **Provide fallbacks** - Cache or degraded functionality
6. **Verify signatures** - For webhook authenticity
7. **Log integration issues** - For debugging
8. **Set rate limits** - Respect upstream service limits
9. **Deduplicate requests** - For in-flight requests
10. **Document contracts** - Keep API integrations documented
