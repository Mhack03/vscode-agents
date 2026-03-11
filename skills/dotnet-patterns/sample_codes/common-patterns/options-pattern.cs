// ============================================================
// Options Pattern — IOptions<T>, IOptionsSnapshot<T>, IOptionsMonitor<T>
// ============================================================

using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

// ── 1. Settings POCO ──────────────────────────────────────────

public class DatabaseSettings
{
    // Convention: expose the config section name as a const
    public const string SectionName = "Database";

    [Required, MinLength(5)]
    public required string ConnectionString { get; init; }

    [Range(1, 500)]
    public int MaxPoolSize { get; init; } = 100;

    public bool EnableSensitiveDataLogging { get; init; } = false;
}

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    [Required, MinLength(32, ErrorMessage = "JWT secret must be at least 32 characters.")]
    public required string Secret { get; init; }

    [Required]
    public required string Issuer { get; init; }

    [Required]
    public required string Audience { get; init; }

    [Range(1, 1440, ErrorMessage = "Token expiry must be 1–1440 minutes.")]
    public int ExpiryMinutes { get; init; } = 60;
}

public class FeatureFlags
{
    public const string SectionName = "FeatureFlags";

    public bool DarkMode { get; init; }
    public bool NewCheckoutFlow { get; init; }
    public bool BetaRecommendations { get; init; }
}

// ── 2. Registration (Program.cs) ─────────────────────────────

// appsettings.json:
// {
//   "Database": {
//     "ConnectionString": "Server=.;Database=MyApp;Trusted_Connection=True;",
//     "MaxPoolSize": 50
//   },
//   "JwtSettings": {
//     "Secret": "super-secret-key-that-is-at-least-32-chars",
//     "Issuer": "https://myapp.example.com",
//     "Audience": "myapp-clients",
//     "ExpiryMinutes": 60
//   },
//   "FeatureFlags": {
//     "DarkMode": true,
//     "NewCheckoutFlow": false
//   }
// }

static class OptionsRegistration
{
    public static IServiceCollection AddAppSettings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind + validate at startup — fails fast if config is missing or invalid
        services.AddOptions<DatabaseSettings>()
            .BindConfiguration(DatabaseSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<JwtSettings>()
            .BindConfiguration(JwtSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // FeatureFlags can change without restart — use IOptionsMonitor
        services.AddOptions<FeatureFlags>()
            .BindConfiguration(FeatureFlags.SectionName);

        return services;
    }
}

// ── 3. Consuming IOptions<T> (Singleton or long-lived services) ──

public class JwtTokenService(IOptions<JwtSettings> options)
{
    // IOptions<T>.Value is resolved once and cached — use for static config
    private readonly JwtSettings _jwt = options.Value;

    public string GenerateToken(string userId, string email, IList<string> roles)
    {
        // Token generation logic uses _jwt.Secret, _jwt.Issuer, etc.
        return $"token-for-{userId}-expires-in-{_jwt.ExpiryMinutes}m"; // Simplified
    }
}

// ── 4. Consuming IOptionsSnapshot<T> (Per-Request, Scoped) ────

public class OrderPricingService(IOptionsSnapshot<FeatureFlags> flags)
{
    // IOptionsSnapshot<T> re-reads config once per HTTP request — good for feature flags
    // in scoped services. Requires the service to be Scoped (not Singleton).
    public decimal CalculateTotal(IEnumerable<OrderLine> lines)
    {
        var subtotal = lines.Sum(l => l.Price * l.Quantity);

        if (flags.Value.NewCheckoutFlow)
        {
            // Apply new discount logic
            return subtotal * 0.95m;
        }

        return subtotal;
    }
}

// ── 5. Consuming IOptionsMonitor<T> (Singleton with live reload) ─

public class FeatureFlagService : IDisposable
{
    private readonly IOptionsMonitor<FeatureFlags> _monitor;
    private readonly IDisposable? _changeListener;
    private readonly ILogger<FeatureFlagService> _logger;

    // IOptionsMonitor<T> is Singleton-safe and supports live config reload
    public FeatureFlagService(
        IOptionsMonitor<FeatureFlags> monitor,
        ILogger<FeatureFlagService> logger)
    {
        _monitor = monitor;
        _logger = logger;

        // Subscribe to changes (e.g., appsettings reload via IConfiguration)
        _changeListener = monitor.OnChange(newFlags =>
            _logger.LogInformation(
                "Feature flags changed: DarkMode={DarkMode}, NewCheckout={NewCheckout}",
                newFlags.DarkMode, newFlags.NewCheckoutFlow));
    }

    public bool IsDarkModeEnabled => _monitor.CurrentValue.DarkMode;
    public bool IsNewCheckoutEnabled => _monitor.CurrentValue.NewCheckoutFlow;
    public bool AreBetaRecsEnabled => _monitor.CurrentValue.BetaRecommendations;

    public void Dispose() => _changeListener?.Dispose();
}

// ── 6. Named Options ──────────────────────────────────────────

public class SmtpSettings
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Sender { get; init; }
}

static class NamedOptionsRegistration
{
    public static IServiceCollection AddMailSettings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register two named instances of the same settings type
        services.Configure<SmtpSettings>("Transactional",
            configuration.GetSection("Email:Transactional"));

        services.Configure<SmtpSettings>("Marketing",
            configuration.GetSection("Email:Marketing"));

        return services;
    }
}

public class EmailDispatcher(IOptionsSnapshot<SmtpSettings> smtp)
{
    public void SendTransactional(string to, string subject, string body)
    {
        var settings = smtp.Get("Transactional");
        // Use settings.Host, settings.Port, settings.Sender...
    }

    public void SendMarketing(string to, string subject, string body)
    {
        var settings = smtp.Get("Marketing");
        // Use settings.Host, settings.Port, settings.Sender...
    }
}

// ── 7. Custom Validation via IValidateOptions<T> ──────────────

public class JwtSettingsValidator : IValidateOptions<JwtSettings>
{
    public ValidateOptionsResult Validate(string? name, JwtSettings options)
    {
        var failures = new List<string>();

        if (!Uri.TryCreate(options.Issuer, UriKind.Absolute, out _))
            failures.Add("Issuer must be an absolute URI.");

        if (!Uri.TryCreate(options.Audience, UriKind.Absolute, out _))
            failures.Add("Audience must be an absolute URI.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

// Registration
// services.AddSingleton<IValidateOptions<JwtSettings>, JwtSettingsValidator>();

// ── Stubs ─────────────────────────────────────────────────────
record OrderLine(Guid ProductId, int Quantity, decimal Price);
