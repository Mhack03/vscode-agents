# Advanced FluentValidation

---

## RuleSets — Group Rules for Selective Validation

```csharp
public class ProductValidator : AbstractValidator<ProductRequest>
{
    public ProductValidator()
    {
        // Default ruleset — always applied
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

        // "Create" ruleset — only on creation
        RuleSet("Create", () =>
        {
            RuleFor(x => x.Sku)
                .NotEmpty()
                .MustAsync(BeUniqueSku).WithMessage("SKU already exists.");
        });

        // "Update" ruleset
        RuleSet("Update", () =>
        {
            RuleFor(x => x.Id).GreaterThan(0);
        });
    }
}

// Validate only specific ruleset
var result = await validator.ValidateAsync(model,
    opts => opts.IncludeRuleSets("Create"), ct);
```

---

## Collection Validation

```csharp
public class OrderValidator : AbstractValidator<CreateOrderRequest>
{
    public OrderValidator(IValidator<OrderLineRequest> lineValidator)
    {
        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("Order must have at least one line.")
            .Must(lines => lines.Count <= 100).WithMessage("Max 100 lines per order.");

        // Validate each element in the collection
        RuleForEach(x => x.Lines).SetValidator(lineValidator);
    }
}

public class OrderLineValidator : AbstractValidator<OrderLineRequest>
{
    public OrderLineValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 999);
        RuleFor(x => x.UnitPrice).GreaterThan(0);
    }
}
```

---

## Custom Rule Extensions

```csharp
public static class ValidatorExtensions
{
    // Reusable rule for slugs
    public static IRuleBuilderOptions<T, string> IsValidSlug<T>(
        this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("'{PropertyName}' must be a valid URL slug (lowercase, hyphens only).");

    // Reusable rule for NZD amounts
    public static IRuleBuilderOptions<T, decimal> IsValidCurrency<T>(
        this IRuleBuilder<T, decimal> ruleBuilder) =>
        ruleBuilder
            .GreaterThan(0)
            .PrecisionScale(18, 2, false)
            .WithMessage("'{PropertyName}' must be a valid currency amount.");
}

// Usage
RuleFor(x => x.Slug).IsValidSlug();
RuleFor(x => x.Price).IsValidCurrency();
```

---

## Conditional Rules

```csharp
// Rule only applied when a condition is met
RuleFor(x => x.CouponCode)
    .NotEmpty()
    .MustAsync(BeValidCoupon)
    .When(x => x.HasCoupon, ApplyConditionTo.CurrentValidator);

// Dependent rules — stop if earlier rules fail
RuleFor(x => x.Email)
    .NotEmpty()
    .EmailAddress()
    .MustAsync(BeUniqueEmail)
    .WithMessage("Email already registered.")
    .DependentRules(() =>
    {
        // Only run these if Email passed all rules above
        RuleFor(x => x.Username)
            .NotEmpty()
            .MustAsync(BeUniqueUsername);
    });
```

---

## Async Database Validators

```csharp
public class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        // Check DB uniqueness
        RuleFor(x => x.Email)
            .MustAsync(async (email, ct) =>
                !await db.Users.AnyAsync(u => u.Email == email, ct))
            .WithMessage("Email address is already registered.");

        // Check via UserManager
        RuleFor(x => x.Username)
            .MustAsync(async (name, ct) =>
                await userManager.FindByNameAsync(name) is null)
            .WithMessage("Username is taken.");

        // Complex cross-field async rule
        RuleFor(x => x)
            .MustAsync(async (req, ct) =>
            {
                if (req.ReferralCode is null) return true;
                return await db.ReferralCodes
                    .AnyAsync(r => r.Code == req.ReferralCode && r.IsActive, ct);
            })
            .WithMessage("Referral code is invalid or expired.")
            .WithName("ReferralCode");
    }
}
```

---

## Validation Result Extensions

```csharp
public static class ValidationResultExtensions
{
    // Convert to Problem Details dictionary
    public static Dictionary<string, string[]> ToDictionary(
        this ValidationResult result) =>
        result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key.ToLowerInvariant(),  // Camel case keys
                g => g.Select(e => e.ErrorMessage).ToArray());

    // Convert to OneOf / Result type
    public static Result<T> ToResult<T>(
        this ValidationResult result, T value) =>
        result.IsValid
            ? Result<T>.Success(value)
            : Result<T>.Failure(result.Errors
                .Select(e => e.ErrorMessage).ToArray());
}
```

---

## Learn More

| Topic | Query |
|-------|-------|
| FluentValidation | `microsoft_docs_search(query="FluentValidation RuleSet RuleForEach collection validator")` |
| Custom validators | `microsoft_docs_search(query="FluentValidation custom property validator IPropertyValidator")` |
