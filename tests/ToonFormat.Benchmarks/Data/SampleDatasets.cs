namespace ToonFormat.Benchmarks.Data;

/// <summary>
/// Sample data models that mirror the official TOON benchmarks.
/// These models represent realistic data structures used in LLM contexts.
/// </summary>
public static class SampleDatasets
{
    /// <summary>
    /// GitHub repository model - mirrors the official TOON benchmark dataset.
    /// </summary>
    public record GitHubRepository(
        int Id,
        string Name,
        string FullName,
        string Description,
        string CreatedAt,
        string UpdatedAt,
        string PushedAt,
        int Stars,
        int Watchers,
        int Forks,
        string DefaultBranch,
        string Language,
        bool IsPrivate,
        bool IsArchived
    );

    /// <summary>
    /// Employee record model - for tabular data benchmarks.
    /// </summary>
    public record Employee(
        int Id,
        string Name,
        string Email,
        string Department,
        string Title,
        decimal Salary,
        string HireDate,
        bool IsActive
    );

    /// <summary>
    /// Daily analytics record - for time series data benchmarks.
    /// </summary>
    public record DailyAnalytics(
        string Date,
        int PageViews,
        int UniqueVisitors,
        int Sessions,
        double BounceRate,
        double AvgSessionDuration,
        int Conversions,
        decimal Revenue
    );

    /// <summary>
    /// Nested user profile - for deep nesting benchmarks.
    /// </summary>
    public record UserProfile(
        int Id,
        string Username,
        PersonalInfo Personal,
        AddressInfo Address,
        PreferencesInfo Preferences,
        List<SocialLink> SocialLinks
    );

    public record PersonalInfo(
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        string DateOfBirth
    );

    public record AddressInfo(
        string Street,
        string City,
        string State,
        string PostalCode,
        string Country,
        GeoLocation? Coordinates
    );

    public record GeoLocation(double Latitude, double Longitude);

    public record PreferencesInfo(
        string Theme,
        string Language,
        bool EmailNotifications,
        bool PushNotifications,
        PrivacySettings Privacy
    );

    public record PrivacySettings(
        bool ProfilePublic,
        bool ShowEmail,
        bool ShowLocation
    );

    public record SocialLink(string Platform, string Url, bool Verified);

    /// <summary>
    /// Simple object for baseline benchmarks.
    /// </summary>
    public record SimpleObject(
        int Id,
        string Name,
        bool Active,
        double Score,
        string Category
    );

    // =====================================================
    // Additional models from official TOON benchmarks
    // =====================================================

    /// <summary>
    /// E-commerce order with nested structures (33% tabular eligibility in official benchmark).
    /// </summary>
    public record EcommerceOrder(
        string OrderId,
        CustomerInfo Customer,
        List<OrderItem> Items,
        string OrderDate,
        string Status,
        decimal Total,
        ShippingInfo? Shipping
    );

    public record CustomerInfo(
        int Id,
        string Name,
        string Email,
        string Phone
    );

    public record OrderItem(
        string ProductId,
        string Name,
        int Quantity,
        decimal Price,
        decimal Subtotal
    );

    public record ShippingInfo(
        string Method,
        string TrackingNumber,
        string EstimatedDelivery,
        AddressInfo Address
    );

    /// <summary>
    /// Event log entry - semi-uniform structure (50% tabular eligibility).
    /// Some events have nested error objects, some don't.
    /// </summary>
    public record EventLog(
        string EventId,
        string Timestamp,
        string Level,
        string Source,
        string Message,
        EventErrorInfo? Error,
        Dictionary<string, object>? Metadata
    );

    public record EventErrorInfo(
        string Code,
        string Message,
        string? StackTrace
    );

    /// <summary>
    /// Deeply nested configuration (0% tabular eligibility).
    /// </summary>
    public record NestedConfig(
        DatabaseConfig Database,
        CacheConfig Cache,
        LoggingConfig Logging,
        SecurityConfig Security,
        FeatureFlags Features
    );

    public record DatabaseConfig(
        string ConnectionString,
        int MaxConnections,
        int Timeout,
        RetryConfig Retry,
        PoolConfig Pool
    );

    public record RetryConfig(int MaxRetries, int DelayMs, bool ExponentialBackoff);
    public record PoolConfig(int MinSize, int MaxSize, int IdleTimeout);

    public record CacheConfig(
        string Provider,
        int DefaultTtlSeconds,
        Dictionary<string, int> TtlOverrides,
        RedisConfig? Redis
    );

    public record RedisConfig(string Host, int Port, string? Password, int Database);

    public record LoggingConfig(
        string Level,
        List<LogSinkConfig> Sinks,
        Dictionary<string, string> CategoryLevels
    );

    public record LogSinkConfig(string Type, string? Path, string? Format);

    public record SecurityConfig(
        AuthConfig Auth,
        CorsConfig Cors,
        RateLimitConfig RateLimit
    );

    public record AuthConfig(string Provider, int TokenExpiryMinutes, List<string> AllowedIssuers);
    public record CorsConfig(List<string> AllowedOrigins, List<string> AllowedMethods, bool AllowCredentials);
    public record RateLimitConfig(int RequestsPerMinute, int BurstLimit, List<string> ExemptPaths);

    public record FeatureFlags(
        bool EnableNewUI,
        bool EnableBetaFeatures,
        Dictionary<string, bool> PerUserFlags,
        RolloutConfig? Rollout
    );

    public record RolloutConfig(int Percentage, List<string> IncludedUsers, List<string> ExcludedUsers);
}
