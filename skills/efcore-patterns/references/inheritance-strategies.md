# EF Core Inheritance Strategies — TPH, TPT, TPC

EF Core supports three strategies for mapping an inheritance hierarchy to relational tables.

---

## Comparison

| Strategy | Tables | Queries | Nulls | Best For |
|----------|--------|---------|-------|---------|
| TPH — Table Per Hierarchy | 1 | Fast (no JOIN) | Many nullable columns | Most hierarchies |
| TPT — Table Per Type | N (one per type) | Slower (JOINs) | No nulls | Large different shapes |
| TPC — Table Per Concrete | N (one per concrete) | Fast (no JOINs) | No nulls | Independent types, EF 7+ |

---

## Base Entity (Shared)

```csharp
public abstract class Payment
{
    public int    Id        { get; set; }
    public decimal Amount   { get; set; }
    public DateTime PaidAt  { get; set; }
    public string  Status   { get; set; } = string.Empty;
}

public class CreditCardPayment : Payment
{
    public string Last4Digits   { get; set; } = string.Empty;
    public string CardBrand     { get; set; } = string.Empty;
}

public class BankTransferPayment : Payment
{
    public string BankName      { get; set; } = string.Empty;
    public string ReferenceCode { get; set; } = string.Empty;
}

public class WalletPayment : Payment
{
    public string WalletProvider { get; set; } = string.Empty;
    public string WalletId       { get; set; } = string.Empty;
}
```

---

## TPH — Table Per Hierarchy (Default)

All types in one table with a discriminator column:

```csharp
modelBuilder.Entity<Payment>()
    .HasDiscriminator<string>("PaymentType")  // Discriminator column
    .HasValue<CreditCardPayment>("CreditCard")
    .HasValue<BankTransferPayment>("BankTransfer")
    .HasValue<WalletPayment>("Wallet");

// Optional: custom discriminator value with enum
modelBuilder.Entity<Payment>()
    .HasDiscriminator(p => p.Type)
    .HasValue<CreditCardPayment>(PaymentType.CreditCard);
```

Resulting table:
```
Payments: Id | Amount | PaidAt | Status | PaymentType | Last4Digits | CardBrand | BankName | ...
```

Nullable columns for type-specific properties are the trade-off.

---

## TPT — Table Per Type

Separate table per type, joined on PK:

```csharp
modelBuilder.Entity<Payment>().ToTable("Payments");
modelBuilder.Entity<CreditCardPayment>().ToTable("CreditCardPayments");
modelBuilder.Entity<BankTransferPayment>().ToTable("BankTransferPayments");
modelBuilder.Entity<WalletPayment>().ToTable("WalletPayments");
```

Resulting tables:
```
Payments:            Id | Amount | PaidAt | Status
CreditCardPayments:  Id | Last4Digits | CardBrand
BankTransferPayments: Id | BankName | ReferenceCode
WalletPayments:      Id | WalletProvider | WalletId
```

SQL query on `Payment` base type generates JOINs to all derived tables — can be slow with many rows.

---

## TPC — Table Per Concrete Type (.NET 7+)

Each concrete type gets its own full table, no shared base table:

```csharp
modelBuilder.Entity<Payment>().UseTpcMappingStrategy();

// Each concrete type gets ALL columns (including base type columns)
// Ideal when you rarely query the base type directly
```

Resulting tables:
```
CreditCardPayments:   Id | Amount | PaidAt | Status | Last4Digits | CardBrand
BankTransferPayments: Id | Amount | PaidAt | Status | BankName | ReferenceCode
WalletPayments:       Id | Amount | PaidAt | Status | WalletProvider | WalletId
```

> **Key limitation**: Identity (auto-increment) PKs don't work with TPC because IDs must be globally unique across tables. Use `Guid` or `HiLo` sequences.

```csharp
// Use HiLo sequence for int PKs with TPC
modelBuilder.HasSequence<int>("PaymentHiLo").StartsAt(1).IncrementsBy(10);
modelBuilder.Entity<Payment>()
    .Property(p => p.Id)
    .HasDefaultValueSql("NEXT VALUE FOR PaymentHiLo");
```

---

## Querying Inheritance

```csharp
// All payments (base type — works with all strategies)
var all = await context.Set<Payment>().ToListAsync();

// Only credit card payments
var cards = await context.Set<CreditCardPayment>().ToListAsync();

// Filter by type
var wallets = await context.Set<Payment>()
    .OfType<WalletPayment>()
    .ToListAsync();

// Type-checking in LINQ
var hasCredit = await context.Set<Payment>()
    .AnyAsync(p => p is CreditCardPayment cc && cc.Last4Digits == "1234");
```

---

## Learn More

| Topic | Query |
|-------|-------|
| TPH/TPT/TPC | `microsoft_docs_fetch(url="https://learn.microsoft.com/en-us/ef/core/modeling/inheritance")` |
| TPC (.NET 7+) | `microsoft_docs_search(query="EF Core table per concrete type TPC mapping strategy")` |
| Discriminator | `microsoft_docs_search(query="EF Core discriminator column TPH HasDiscriminator")` |
