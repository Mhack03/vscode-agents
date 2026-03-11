# C# 12 / 13 Modern Features

Reference for C# language features available in .NET 8/9/10.

---

## C# 12 Features (.NET 8)

### Primary Constructors on Non-Record Types

Before C# 12, primary constructors existed only on `record`. Now any class or struct can use them.

```csharp
// Old pattern
public class ProductService
{
    private readonly IProductRepository _repo;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository repo, ILogger<ProductService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }
}

// C# 12 — primary constructor, parameters captured by the class body
public class ProductService(IProductRepository repo, ILogger<ProductService> logger)
{
    public async Task<Product?> FindAsync(Guid id)
    {
        logger.LogInformation("Finding product {Id}", id);
        return await repo.FindByIdAsync(id);
    }
}
```

> **Note**: Primary constructor parameters are NOT automatically stored as fields. If you need a field (e.g., for mutation), capture explicitly: `private readonly IProductRepository _repo = repo;`

---

### Collection Expressions

Unified literal syntax for arrays, lists, spans, and other collection types:

```csharp
// Arrays
int[] fibonacci = [1, 1, 2, 3, 5, 8, 13];

// List<T>
List<string> roles = ["Admin", "User", "Guest"];

// Span<T> — stack allocated, no heap allocation
Span<byte> header = [0x47, 0x49, 0x46];

// ImmutableArray<T>
ImmutableArray<int> primes = [2, 3, 5, 7, 11];

// Spread operator (..) — flattens a collection inline
int[] first  = [1, 2, 3];
int[] second = [4, 5, 6];
int[] all    = [..first, ..second, 7];  // [1,2,3,4,5,6,7]

// Works in return positions
return [item1, item2, item3];
```

---

### Default Lambda Parameters

```csharp
var greet = (string name, string greeting = "Hello") => $"{greeting}, {name}!";
Console.WriteLine(greet("Alice"));          // Hello, Alice!
Console.WriteLine(greet("Bob", "Hi"));      // Hi, Bob!
```

---

### Inline Arrays (High-Performance Buffers)

```csharp
[System.Runtime.CompilerServices.InlineArray(8)]
public struct Buffer8<T>
{
    private T _element;
}

// Used like a fixed-size stack-allocated array
var buf = new Buffer8<int>();
buf[0] = 42;
```

---

### `ref readonly` Parameters

```csharp
// Passes by reference (avoids copy) but guarantees no mutation
void Process(ref readonly LargeStruct data)
{
    Console.WriteLine(data.Value); // OK
    // data.Value = 1;             // Compile error — readonly
}
```

---

## C# 13 Features (.NET 9)

### `params` Collections

`params` now works with any collection type, not just arrays:

```csharp
void Log(string message, params IEnumerable<string> tags)
{
    Console.WriteLine($"[{string.Join(", ", tags)}] {message}");
}

Log("User created", "audit", "users", "create");
Log("Order placed", ["order", "commerce"]);
```

---

### New Lock Type (`System.Threading.Lock`)

A more efficient lock that compiles to better IL:

```csharp
private readonly Lock _lock = new();

void SafeWrite(int value)
{
    using (_lock.EnterScope())  // or: lock (_lock) { ... }
    {
        _data = value;
    }
}
```

---

### `\e` Escape Sequence

Shorthand for the ESC character (`\u001B`) used in ANSI terminal codes:

```csharp
Console.WriteLine("\e[31mRed text\e[0m");  // Equivalent to \u001B[31m...
```

---

### `field` Keyword (Preview in C# 14)

Allows property accessors to reference the compiler-generated backing field without declaring it:

```csharp
public string Name
{
    get => field;
    set => field = value?.Trim() ?? string.Empty;
}
```

---

## Records: Full Reference

### Positional Records

```csharp
// Compiler generates: constructor, init-only properties, Deconstruct, Equals, GetHashCode, ToString
public record Point(double X, double Y);

var p1 = new Point(1.0, 2.0);
var p2 = p1 with { Y = 5.0 };     // Non-destructive mutation
var (x, y) = p1;                   // Deconstruct

Console.WriteLine(p1 == new Point(1.0, 2.0)); // true — value equality
```

### Nominal Records

```csharp
public record Person
{
    public required string FirstName { get; init; }
    public required string LastName  { get; init; }
    public string FullName => $"{FirstName} {LastName}";
}
```

### Record Struct (Value Type)

```csharp
// Stack-allocated value semantics — good for small DTOs in hot paths
public readonly record struct Coordinate(double Lat, double Lon);
```

### When to Use Records vs Classes

| Use `record` | Use `class` |
|--------------|-------------|
| Immutable DTOs (API request/response) | Entities with mutable state |
| Value objects in DDD | Services and repositories |
| Configuration snapshots | Domain logic with identity |
| CQRS commands/queries | Event handlers |

---

## Pattern Matching: Full Reference

### Switch Expression

```csharp
string Describe(Shape shape) => shape switch
{
    Circle  { Radius: > 10 } c  => $"Large circle, area {Math.PI * c.Radius * c.Radius:F2}",
    Circle  c                   => $"Small circle r={c.Radius}",
    Rectangle { Width: var w, Height: var h } when w == h => "Square",
    Rectangle r                 => $"Rectangle {r.Width}x{r.Height}",
    null                        => throw new ArgumentNullException(nameof(shape)),
    _                           => "Unknown shape"
};
```

### Property Pattern

```csharp
bool IsVip(Customer c) => c is { Tier: CustomerTier.Gold, IsActive: true };
```

### List Pattern (C# 11+)

```csharp
bool StartsWithOne(int[] arr) => arr is [1, ..];
bool HasExactlyTwo(int[] arr) => arr is [_, _];
bool HasThreeOrMore(int[] arr) => arr is [_, _, _, ..];
```

### Tuple Pattern

```csharp
string RpsWinner(string a, string b) => (a, b) switch
{
    ("Rock",     "Scissors") => "A",
    ("Scissors", "Paper")    => "A",
    ("Paper",    "Rock")     => "A",
    var (x, y) when x == y   => "Draw",
    _                        => "B"
};
```

---

## Nullable Reference Types: Full Reference

### Enable per project

```xml
<!-- MyApp.csproj -->
<PropertyGroup>
  <Nullable>enable</Nullable>    <!-- Recommended for all new projects -->
  <WarningsAsErrors>nullable</WarningsAsErrors>  <!-- Treat nullable warnings as errors -->
</PropertyGroup>
```

### Annotations

| Annotation | Meaning |
|------------|---------|
| `string`   | Never null — compiler warns if null assigned |
| `string?`  | May be null — must null-check before dereference |
| `T?`       | Nullable value type (same as `Nullable<T>`) when T is struct |
| `[NotNull]` | Output guaranteed non-null (annotation attribute) |
| `[MaybeNull]` | Output may be null even if return type says non-null |

### Common Patterns

```csharp
// Null-conditional operator
int? length = name?.Length;

// Null-coalescing
string display = name ?? "Anonymous";

// Null-coalescing assignment
name ??= "Default";

// Null-forgiving operator (suppress warning — use sparingly)
string definitelyNotNull = PossiblyNullMethod()!;

// Guard clauses (preferred over null-forgiving)
ArgumentNullException.ThrowIfNull(user);
```

---

## `required` Members (.NET 7+)

```csharp
public class UserDto
{
    public required string Email    { get; init; }  // Must be set in object initializer
    public required string Username { get; init; }
    public string? Bio { get; init; }               // Optional
}

// ✅ OK
var user = new UserDto { Email = "a@b.com", Username = "alice" };

// ❌ Compile error — Email and Username not provided
var bad = new UserDto();
```

---

## `init` Accessors

```csharp
public class Order
{
    public Guid Id { get; init; } = Guid.NewGuid();  // Set only during construction
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";  // Mutable after construction
}
```

---

## Learn More

| Topic | Search Query |
|-------|-------------|
| C# 12 full feature list | `microsoft_docs_fetch(url="https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12")` |
| C# 13 features | `microsoft_docs_fetch(url="https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-13")` |
| Pattern matching | `microsoft_docs_search(query="C# pattern matching switch expression")` |
| Records deep dive | `microsoft_docs_search(query="C# record types immutable")` |
| Nullable analysis | `microsoft_docs_search(query="C# nullable reference types enable annotations")` |
