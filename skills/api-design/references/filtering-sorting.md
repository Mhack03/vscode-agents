# Filtering, Sorting & Searching Implementation

## Query Parameters

```
GET /api/users?status=active&role=admin
GET /api/users?sort=createdAt:desc,name:asc
GET /api/users?search=john&searchFields=name,email
GET /api/users?status=active&sort=createdAt:desc&page=1&limit=20
```

## Express.js Implementation

```javascript
app.get("/api/users", async (req, res) => {
	const {
		status,
		role,
		sort = "createdAt:desc",
		page = 1,
		limit = 20,
		search,
		searchFields = "name,email",
	} = req.query;

	// Build query
	const query = {};
	if (status) query.status = status;
	if (role) query.role = role;

	// Search
	if (search) {
		const fields = searchFields.split(",").map((f) => f.trim());
		query.$or = fields.map((field) => ({
			[field]: { $regex: search, $options: "i" },
		}));
	}

	// Sort
	const sortObj = {};
	sort.split(",").forEach((s) => {
		const [field, order] = s.trim().split(":");
		sortObj[field] = order === "desc" ? -1 : 1;
	});

	// Execute query
	const users = await User.find(query)
		.sort(sortObj)
		.skip((page - 1) * limit)
		.limit(parseInt(limit));

	const total = await User.countDocuments(query);

	res.json({
		data: users,
		pagination: {
			page: parseInt(page),
			limit: parseInt(limit),
			total,
			totalPages: Math.ceil(total / limit),
			hasNext: page * limit < total,
			hasPrevious: page > 1,
		},
	});
});
```

## ASP.NET Core Implementation

```csharp
[HttpGet]
public async Task<IActionResult> GetUsers(
    [FromQuery] string? status,
    [FromQuery] string? role,
    [FromQuery] string sort = "createdAt:desc",
    [FromQuery] int page = 1,
    [FromQuery] int limit = 20,
    [FromQuery] string? search)
{
    IQueryable<User> query = _context.Users;

    // Filter
    if (!string.IsNullOrEmpty(status))
        query = query.Where(u => u.Status == status);

    if (!string.IsNullOrEmpty(role))
        query = query.Where(u => u.Role == role);

    // Search
    if (!string.IsNullOrEmpty(search))
        query = query.Where(u =>
            u.Name.Contains(search) ||
            u.Email.Contains(search));

    // Sort
    var (field, order) = ParseSort(sort);
    query = ApplySort(query, field, order);

    // Pagination
    var total = await query.CountAsync();
    var users = await query
        .Skip((page - 1) * limit)
        .Take(limit)
        .ToListAsync();

    return Ok(new
    {
        data = users,
        pagination = new
        {
            page,
            limit,
            total,
            totalPages = (int)Math.Ceiling((double)total / limit),
            hasNext = page * limit < total,
            hasPrevious = page > 1
        }
    });
}

private (string field, string order) ParseSort(string sort)
{
    var parts = sort.Split(':');
    return (parts[0], parts.Length > 1 ? parts[1] : "asc");
}

private IQueryable<User> ApplySort(IQueryable<User> query, string field, string order)
{
    return field.ToLower() switch
    {
        "name" => order == "desc" ? query.OrderByDescending(u => u.Name) : query.OrderBy(u => u.Name),
        "createdat" => order == "desc" ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
        "email" => order == "desc" ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
        _ => query.OrderBy(u => u.Id)
    };
}
```

## Validation for Query Parameters

```javascript
const { body, query, validationResult } = require("express-validator");

app.get(
	"/api/users",
	query("page").optional().isInt({ min: 1 }),
	query("limit").optional().isInt({ min: 1, max: 100 }),
	query("status").optional().isIn(["active", "inactive", "pending"]),
	query("sort")
		.optional()
		.matches(/^[a-zA-Z]+:(asc|desc)(,[a-zA-Z]+:(asc|desc))*$/),
	(req, res, next) => {
		const errors = validationResult(req);
		if (!errors.isEmpty()) {
			return res.status(422).json({
				error: {
					code: "VALIDATION_ERROR",
					message: "Invalid query parameters",
					details: errors.array(),
				},
			});
		}
		next();
	},
	getUsersController
);
```

## Common Patterns

### Multiple Filters

```
GET /api/products?category=electronics&brand=apple,samsung&priceMin=100&priceMax=1000
```

### Search Specific Fields

```
GET /api/users?search=john&searchFields=firstName,lastName
```

### Exclude Fields

```
GET /api/users?exclude=password,secretToken
```

### Include Relations

```
GET /api/users?include=orders,profile
```

## Best Practices

1. **Whitelist allowed fields** - Prevent injection attacks
2. **Limit query depth** - Cap sorting/filtering complexity
3. **Validate parameters** - Use validators like Express Validator
4. **Document parameters** - Include in OpenAPI/Swagger
5. **Set reasonable defaults** - Use sensible page size/sort
6. **Index database fields** - For performance on large datasets
7. **Use cursor pagination** - For very large result sets
