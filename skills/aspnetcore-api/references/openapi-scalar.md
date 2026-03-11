# OpenAPI & Scalar Setup — ASP.NET Core

.NET 9+ ships with `Microsoft.AspNetCore.OpenApi` built in. No Swashbuckle required, though it remains an option.

---

## .NET 9+ Built-In OpenAPI

```csharp
// Program.cs
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

var app = builder.Build();

// Serves /openapi/v1.json (only expose in non-production)
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
```

---

## Scalar UI (Modern Swagger Alternative)

```bash
dotnet add package Scalar.AspNetCore
```

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title  = "My API";
        options.Theme  = ScalarTheme.Purple;
        options.DefaultHttpClient = (ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}
// Open: https://localhost:5001/scalar/v1
```

---

## Add JWT Bearer Security to OpenAPI

```csharp
// Transformers/BearerSecuritySchemeTransformer.cs
public class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken ct)
    {
        var schemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!schemes.Any(s => s.Name == JwtBearerDefaults.AuthenticationScheme))
            return;

        var securityScheme = new OpenApiSecurityScheme
        {
            Type   = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In     = ParameterLocation.Header,
            Name   = "Authorization"
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes.Add("Bearer", securityScheme);

        foreach (var operation in document.Paths.Values
            .SelectMany(p => p.Operations.Values))
        {
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Id   = "Bearer",
                        Type = ReferenceType.SecurityScheme
                    }
                }] = Array.Empty<string>()
            });
        }
    }
}
```

---

## Endpoint Metadata for OpenAPI

```csharp
// Add rich metadata to each endpoint
products.MapPost("/", CreateProduct)
    .WithName("CreateProduct")
    .WithSummary("Create a new product")
    .WithDescription("Creates a product and returns 201 Created with Location header.")
    .WithTags("Products")
    .Produces<ProductDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .WithOpenApi(op =>
    {
        op.Parameters[0].Description = "The product to create.";
        return op;
    });
```

---

## Versioned OpenAPI Documents

```csharp
// Multiple OpenAPI documents — one per API version
builder.Services.AddOpenApi("v1", options => options.AddDocumentTransformer(...));
builder.Services.AddOpenApi("v2", options => options.AddDocumentTransformer(...));

app.MapOpenApi("/openapi/{documentName}.json");
app.MapScalarApiReference(o => o.Servers = []);
```

---

## Swashbuckle (Alternative, .NET 8 Default Template)

```bash
dotnet add package Swashbuckle.AspNetCore
```

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type   = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
});

app.UseSwagger();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1"));
```

---

## Learn More

| Topic | Query |
|-------|-------|
| Built-in OpenAPI | `microsoft_docs_fetch(url="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview")` |
| Scalar | `microsoft_docs_search(query="scalar aspnet core openapi UI")` |
| Document transformers | `microsoft_docs_search(query="aspnet core openapi document transformer IOpenApiDocumentTransformer")` |
