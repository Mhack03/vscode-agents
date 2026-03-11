---
name: api-design
description: Design and implement RESTful and GraphQL APIs with pagination, versioning, authentication, rate limiting, error handling, and performance optimization. Use when building API endpoints, integrating third-party services, implementing API security, or optimizing API performance.
license: Complete terms in LICENSE.txt
---

# API Design & Integration

## When to Use This Skill

- Designing REST or GraphQL API endpoints
- Implementing API versioning and deprecation
- Setting up authentication (JWT, OAuth, API keys)
- Adding rate limiting and caching
- Handling API errors consistently
- Integrating third-party APIs
- Optimizing query performance
- Building API documentation

## RESTful API Fundamentals

### Resource-Based URLs

```
✅ Good - Resource-oriented
GET    /api/users              # List users
POST   /api/users              # Create user
GET    /api/users/123          # Get user
PUT    /api/users/123          # Replace user
PATCH  /api/users/123          # Partial update
DELETE /api/users/123          # Delete user

❌ Bad - Action-oriented
GET    /api/getAllUsers
POST   /api/createUser
GET    /api/getUserById/123
```

### Key HTTP Methods

| Method | Idempotent | Cacheable | Use Case                        |
| ------ | ---------- | --------- | ------------------------------- |
| GET    | ✓          | ✓         | Retrieve data (no side effects) |
| POST   | ✗          | ✗         | Create new resource             |
| PUT    | ✓          | ✗         | Replace entire resource         |
| PATCH  | ✓          | ✗         | Partial updates                 |
| DELETE | ✓          | ✗         | Remove resource                 |

### HTTP Status Codes

- `200` OK, `201` Created, `202` Accepted, `204` No Content
- `400` Bad Request, `401` Unauthorized, `403` Forbidden, `404` Not Found
- `409` Conflict, `422` Unprocessable Entity, `429` Too Many Requests
- `500` Internal Error, `502` Bad Gateway, `503` Service Unavailable

### Consistent Response Format

```javascript
// Success response
{
  "data": { "id": 123, "name": "John", "email": "john@example.com" },
  "meta": { "timestamp": "2026-02-12T10:00:00Z" }
}

// Error response
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid email format",
    "details": [{ "field": "email", "message": "Must be valid" }]
  },
  "meta": { "timestamp": "2026-02-12T10:00:00Z", "requestId": "abc-123" }
}
```

## Pagination & Filtering

### Offset-Based Pagination

```javascript
GET /api/users?page=2&limit=20

// Response
{
  "data": [...],
  "pagination": {
    "page": 2,
    "limit": 20,
    "total": 100,
    "totalPages": 5,
    "hasNext": true
  }
}
```

### Cursor-Based Pagination (Better for large datasets)

```javascript
GET /api/users?cursor=eyJpZCI6MTIzfQ&limit=20

// Response includes nextCursor for pagination
```

### Filtering & Sorting

```
GET /api/users?status=active&sort=createdAt:desc&limit=20
GET /api/users?search=john&searchFields=name,email
GET /api/users?role=admin,moderator  // Multiple values
```

For detailed implementation examples, see [filtering-sorting](/references/filtering-sorting.md).

## Security & Authentication

### JWT Authentication Pattern

```javascript
// Generate token
const token = jwt.sign(
	{ userId: user.id, role: user.role },
	process.env.JWT_SECRET,
	{ expiresIn: "1h" }
);

// Middleware
async function authenticate(req, res, next) {
	const token = req.header("Authorization")?.replace("Bearer ", "");
	const decoded = jwt.verify(token, process.env.JWT_SECRET);
	req.user = await User.findById(decoded.userId);
	next();
}
```

### Authorization

```javascript
function authorize(...roles) {
	return (req, res, next) => {
		if (!roles.includes(req.user.role)) {
			throw new Error("Insufficient permissions");
		}
		next();
	};
}

app.get("/api/users", authenticate, authorize("admin"), controller);
```

### API Key Authentication

```javascript
function validateAPIKey(req) {
	const key = req.header("X-API-Key");
	return APIKey.findOne({ key: hash(key), isActive: true });
}
```

For comprehensive patterns, see [authentication](/references/authentication.md).

## Error Handling

```javascript
class APIError extends Error {
	constructor(statusCode, code, message, details = []) {
		super(message);
		this.statusCode = statusCode;
		this.code = code;
		this.details = details;
	}
}

// Global error handler
app.use((err, req, res, next) => {
	res.status(err.statusCode || 500).json({
		error: {
			code: err.code || "INTERNAL_ERROR",
			message: err.message,
			...(err.details && { details: err.details }),
		},
		meta: { timestamp: new Date().toISOString(), requestId: req.id },
	});
});
```

## API Versioning

### URL Versioning (Recommended)

```
GET /api/v1/users
GET /api/v2/users
```

### Header Versioning

```
GET /api/users
Accept: application/vnd.api.v1+json
```

### Deprecation Headers

```javascript
res.set("Warning", '299 - "Use /api/v2/users instead"');
res.set("Sunset", "Sat, 31 Dec 2026 23:59:59 GMT");
```

## Rate Limiting

```javascript
const rateLimit = require("express-rate-limit");

const limiter = rateLimit({
	windowMs: 15 * 60 * 1000, // 15 minutes
	max: 100, // 100 requests per window
	message: { error: { code: "RATE_LIMIT_EXCEEDED" } },
});

app.use("/api/", limiter);
```

For advanced patterns (per-user, circuit breaker), see [rate-limiting](/references/rate-limiting.md).

## Caching Strategies

### Cache Middleware Pattern

```javascript
function cacheMiddleware(duration = 300) {
	return async (req, res, next) => {
		const cached = await redis.get(`cache:${req.url}`);
		if (cached) return res.json(JSON.parse(cached));

		const originalJson = res.json.bind(res);
		res.json = (data) => {
			redis.setex(`cache:${req.url}`, duration, JSON.stringify(data));
			return originalJson(data);
		};
		next();
	};
}

app.get("/api/products", cacheMiddleware(600), controller);
```

For Redis integration and HybridCache patterns, see [caching](/references/caching.md).

## API Documentation

### OpenAPI/Swagger Setup

```javascript
const swaggerJsdoc = require("swagger-jsdoc");
const swaggerUi = require("swagger-ui-express");

const spec = swaggerJsdoc({
	definition: {
		openapi: "3.0.0",
		info: { title: "My API", version: "1.0.0" },
		components: {
			securitySchemes: {
				bearerAuth: { type: "http", scheme: "bearer", bearerFormat: "JWT" },
			},
		},
	},
	apis: ["./routes/*.js"],
});

app.use("/api-docs", swaggerUi.serve, swaggerUi.setup(spec));
```

Annotate routes with JSDoc `@swagger` comments for auto-documentation.

## Third-Party API Integration

### Retry Logic with Exponential Backoff

```javascript
async function fetchWithRetry(url, maxRetries = 3) {
	for (let attempt = 0; attempt <= maxRetries; attempt++) {
		try {
			const response = await fetch(url, { timeout: 10000 });
			if (!response.ok && (response.status >= 500 || response.status === 429)) {
				if (attempt < maxRetries) {
					await sleep(Math.min(1000 * Math.pow(2, attempt), 10000));
					continue;
				}
			}
			return response.json();
		} catch (error) {
			if (attempt === maxRetries) throw error;
		}
	}
}
```

### Circuit Breaker Pattern

See [external-apis](/references/external-apis.md) for complete implementation.

## GraphQL APIs

### Schema Example

```graphql
type User {
	id: ID!
	email: String!
	orders: [Order!]!
}

type Query {
	user(id: ID!): User
	users(page: Int, limit: Int): UserConnection!
}

type Mutation {
	createUser(email: String!, name: String!): User!
}
```

See [graphql-apis](/references/graphql-apis.md) for resolvers and best practices.

## Testing APIs

```javascript
const request = require("supertest");

describe("User API", () => {
	test("GET /api/users returns users list", async () => {
		const res = await request(app)
			.get("/api/users")
			.set("Authorization", `Bearer ${token}`)
			.expect(200);

		expect(res.body.data).toBeArray();
	});
});
```

## Quick Checklist

- [ ] Resource-based URL structure
- [ ] Correct HTTP methods & status codes
- [ ] Consistent response format
- [ ] Error handling with Problem Details
- [ ] Input validation (FluentValidation or Zod)
- [ ] Authentication (JWT/OAuth)
- [ ] Authorization policies
- [ ] Rate limiting configured
- [ ] API versioning strategy
- [ ] Pagination for list endpoints
- [ ] Caching for expensive queries
- [ ] API documentation (OpenAPI)
- [ ] CORS configured
- [ ] Request logging
- [ ] Test coverage (unit + integration)
