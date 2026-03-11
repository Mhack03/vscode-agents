# Request Models & Validation Binding

---

## Minimal API Binding Sources

```csharp
// Explicit binding from different sources
app.MapPost("/api/orders/{customerId}", async (
    [FromRoute]  string              customerId,     // /api/orders/{customerId}
    [FromQuery]  string?             couponCode,     // ?couponCode=SAVE10
    [FromHeader] string?             idempotencyKey, // Idempotency-Key: abc123
    [FromBody]   CreateOrderRequest  body,           // JSON body
    AppDbContext db) => { });

// [AsParameters] — bind multiple properties from a record (Minimal API)
app.MapGet("/api/products", async ([AsParameters] GetProductsQuery query,
    IProductService svc, CancellationToken ct) =>
    TypedResults.Ok(await svc.GetAsync(query, ct)));

public record GetProductsQuery(
    [FromQuery] string? SearchTerm,
    [FromQuery] string? Category,
    [FromQuery] int     Page  = 1,
    [FromQuery] int     Size  = 25,
    [FromQuery] string  Sort  = "name_asc");
```

---

## Record vs Class for Request Models

```csharp
// ✅ Preferred: immutable record — value equality, concise, thread-safe
public record CreateProductRequest(
    string  Name,
    string  Sku,
    decimal Price,
    int     CategoryId,
    string? Description = null);

// Use a class when you need mutable properties or custom constructors
public class UpdateProductRequest
{
    public string?  Name        { get; set; }
    public decimal? Price       { get; set; }
    public string?  Description { get; set; }
}
```

---

## Data Annotations (for simpler cases)

```csharp
// Useful for basic constraints without FluentValidation
public record RegisterUserRequest(
    [Required, EmailAddress, MaxLength(150)]
    string Email,

    [Required, MinLength(12), MaxLength(100),
     RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*\W).+$",
         ErrorMessage = "Password must contain uppercase, lowercase, digit, and special char.")]
    string Password,

    [Required, MaxLength(50)]
    string FirstName,

    [Required, MaxLength(50)]
    string LastName,

    [Range(13, 120)]
    int? Age = null);
```

---

## Custom Model Binder (advanced)

Useful for custom types not handled by default binders:

```csharp
// Custom binder for comma-separated int list: ?ids=1,2,3,4
public class IntListBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext ctx)
    {
        var value = ctx.ValueProvider.GetValue(ctx.ModelName);
        if (value == ValueProviderResult.None)
        {
            ctx.Result = ModelBindingResult.Success(new List<int>());
            return Task.CompletedTask;
        }

        var ids = value.FirstValue?
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var i) ? i : -1)
            .Where(i => i > 0)
            .ToList() ?? [];

        ctx.Result = ModelBindingResult.Success(ids);
        return Task.CompletedTask;
    }
}

// Apply via attribute or globally
public record BulkDeleteRequest(
    [ModelBinder(typeof(IntListBinder))] List<int> Ids);
```

---

## Strongly Typed File Upload

```csharp
public record UploadAvatarRequest(
    IFormFile File);

app.MapPost("/api/users/{id}/avatar", async (
    int id, [FromForm] UploadAvatarRequest req, IStorageService storage,
    ClaimsPrincipal user, CancellationToken ct) =>
{
    const long maxSize = 5 * 1024 * 1024;   // 5 MB
    string[]   allowed = ["image/jpeg", "image/png", "image/webp"];

    if (req.File.Length > maxSize)
        return TypedResults.Problem("File exceeds 5 MB limit.", statusCode: 400);

    if (!allowed.Contains(req.File.ContentType))
        return TypedResults.Problem("Unsupported file type.", statusCode: 415);

    await using var stream = req.File.OpenReadStream();
    var url = await storage.UploadAsync($"avatars/{id}", stream, req.File.ContentType, ct);
    return TypedResults.Ok(new { avatarUrl = url });
})
.DisableAntiforgery();   // Required for multipart/form-data
```

---

## Request Size Limits

```csharp
// Global limit
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50 * 1024 * 1024;  // 50 MB
});

// Per-endpoint override
app.MapPost("/api/import", Handle)
   .WithMetadata(new RequestSizeLimitAttribute(100 * 1024 * 1024));   // 100 MB
```

---

## Learn More

| Topic | Query |
|-------|-------|
| Minimal API binding | `microsoft_docs_search(query="ASP.NET Core Minimal API model binding AsParameters FromBody")` |
| Custom binders | `microsoft_docs_search(query="ASP.NET Core custom IModelBinder IModelBinderProvider")` |
| File upload | `microsoft_docs_search(query="ASP.NET Core file upload IFormFile Minimal API streaming")` |
