using static ToonFormat.Benchmarks.Data.SampleDatasets;

namespace ToonFormat.Benchmarks.Data;

/// <summary>
/// Generates realistic test data for benchmarks.
/// Data patterns mirror the official TOON benchmark datasets.
/// 
/// Official benchmark sizes (from toon-format/toon):
/// - Employees: 100 records (uniform tabular)
/// - Analytics: 60 days (time-series, uniform)
/// - GitHub Repos: 100 repositories (uniform)
/// - User Profiles: 50 nested objects (deep nesting)
/// - E-commerce Orders: 50 orders (nested structures, 33% tabular eligibility)
/// - Event Logs: 75 logs (semi-uniform, 50% tabular eligibility)
/// - Nested Config: 1 deep config (0% tabular eligibility)
/// </summary>
public static class BenchmarkDataGenerator
{
    // === Official benchmark sizes ===
    public const int OfficialEmployeeCount = 100;
    public const int OfficialAnalyticsDays = 60;
    public const int OfficialRepoCount = 100;
    public const int OfficialUserProfileCount = 50;
    public const int OfficialOrderCount = 50;
    public const int OfficialEventLogCount = 75;

    private static readonly string[] Departments = 
        ["Engineering", "Sales", "Marketing", "HR", "Finance", "Operations", "Legal", "Support"];
    
    private static readonly string[] Titles = 
        ["Engineer", "Manager", "Director", "VP", "Analyst", "Specialist", "Lead", "Associate"];
    
    private static readonly string[] Languages = 
        ["TypeScript", "Python", "Go", "Rust", "C#", "Java", "JavaScript", "Ruby"];
    
    private static readonly string[] Categories = 
        ["A", "B", "C", "D", "E"];

    private static readonly string[] Themes = ["light", "dark", "system"];
    private static readonly string[] LanguageCodes = ["en", "es", "fr", "de", "ja", "zh"];
    private static readonly string[] Platforms = ["twitter", "github", "linkedin", "website"];

    /// <summary>
    /// Generates a list of GitHub repository records.
    /// </summary>
    public static List<GitHubRepository> GenerateGitHubRepositories(int count)
    {
        var repos = new List<GitHubRepository>(count);
        var baseDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (int i = 0; i < count; i++)
        {
            var createdAt = baseDate.AddDays(i * 10).ToString("O");
            var updatedAt = baseDate.AddDays(i * 10 + 30).ToString("O");
            var pushedAt = baseDate.AddDays(i * 10 + 25).ToString("O");

            repos.Add(new GitHubRepository(
                Id: 1000000 + i,
                Name: $"project-{i:D4}",
                FullName: $"org-{i % 10}/project-{i:D4}",
                Description: $"A sample project for benchmarking TOON format. This is repository number {i} with various features and capabilities for testing serialization performance.",
                CreatedAt: createdAt,
                UpdatedAt: updatedAt,
                PushedAt: pushedAt,
                Stars: (i * 137) % 50000,
                Watchers: (i * 23) % 1000,
                Forks: (i * 47) % 5000,
                DefaultBranch: i % 3 == 0 ? "main" : "master",
                Language: Languages[i % Languages.Length],
                IsPrivate: i % 5 == 0,
                IsArchived: i % 20 == 0
            ));
        }

        return repos;
    }

    /// <summary>
    /// Generates a list of employee records.
    /// </summary>
    public static List<Employee> GenerateEmployees(int count)
    {
        var employees = new List<Employee>(count);
        var baseDate = new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (int i = 0; i < count; i++)
        {
            var hireDate = baseDate.AddDays(i * 7).ToString("yyyy-MM-dd");

            employees.Add(new Employee(
                Id: 10000 + i,
                Name: $"Employee {i:D4}",
                Email: $"employee{i:D4}@company.com",
                Department: Departments[i % Departments.Length],
                Title: $"{Titles[i % Titles.Length]} {(i % 3) + 1}",
                Salary: 50000m + (i * 500m) + (i % 100) * 100m,
                HireDate: hireDate,
                IsActive: i % 10 != 0
            ));
        }

        return employees;
    }

    /// <summary>
    /// Generates daily analytics records for a specified number of days.
    /// </summary>
    public static List<DailyAnalytics> GenerateDailyAnalytics(int days)
    {
        var analytics = new List<DailyAnalytics>(days);
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (int i = 0; i < days; i++)
        {
            var date = baseDate.AddDays(i).ToString("yyyy-MM-dd");
            var baseViews = 10000 + (int)(Math.Sin(i * 0.1) * 2000) + (i % 7 < 5 ? 3000 : 0);

            analytics.Add(new DailyAnalytics(
                Date: date,
                PageViews: baseViews + (i * 17) % 1000,
                UniqueVisitors: (int)(baseViews * 0.6) + (i * 13) % 500,
                Sessions: (int)(baseViews * 0.8) + (i * 11) % 700,
                BounceRate: 30.0 + (i % 20) + Math.Sin(i * 0.2) * 5,
                AvgSessionDuration: 120.0 + (i % 60) + Math.Cos(i * 0.15) * 30,
                Conversions: (int)(baseViews * 0.02) + (i % 50),
                Revenue: 1000m + (i * 50m) + ((i % 30) * 10m)
            ));
        }

        return analytics;
    }

    /// <summary>
    /// Generates nested user profiles for deep structure benchmarks.
    /// </summary>
    public static List<UserProfile> GenerateUserProfiles(int count)
    {
        var profiles = new List<UserProfile>(count);

        for (int i = 0; i < count; i++)
        {
            profiles.Add(new UserProfile(
                Id: 100000 + i,
                Username: $"user_{i:D5}",
                Personal: new PersonalInfo(
                    FirstName: $"First{i}",
                    LastName: $"Last{i}",
                    Email: $"user{i}@example.com",
                    Phone: $"+1-555-{(1000 + i % 9000):D4}",
                    DateOfBirth: new DateTime(1980 + (i % 30), (i % 12) + 1, (i % 28) + 1).ToString("yyyy-MM-dd")
                ),
                Address: new AddressInfo(
                    Street: $"{100 + i} Main Street",
                    City: $"City{i % 50}",
                    State: $"ST",
                    PostalCode: $"{10000 + i:D5}",
                    Country: "USA",
                    Coordinates: i % 3 == 0 ? new GeoLocation(40.7128 + (i * 0.01), -74.0060 + (i * 0.01)) : null
                ),
                Preferences: new PreferencesInfo(
                    Theme: Themes[i % Themes.Length],
                    Language: LanguageCodes[i % LanguageCodes.Length],
                    EmailNotifications: i % 2 == 0,
                    PushNotifications: i % 3 == 0,
                    Privacy: new PrivacySettings(
                        ProfilePublic: i % 4 != 0,
                        ShowEmail: i % 5 == 0,
                        ShowLocation: i % 6 == 0
                    )
                ),
                SocialLinks: GenerateSocialLinks(i)
            ));
        }

        return profiles;
    }

    private static List<SocialLink> GenerateSocialLinks(int seed)
    {
        var links = new List<SocialLink>();
        var count = (seed % 4) + 1;

        for (int i = 0; i < count; i++)
        {
            var platform = Platforms[(seed + i) % Platforms.Length];
            links.Add(new SocialLink(
                Platform: platform,
                Url: $"https://{platform}.com/user{seed}",
                Verified: (seed + i) % 3 == 0
            ));
        }

        return links;
    }

    /// <summary>
    /// Generates simple objects for baseline benchmarks.
    /// </summary>
    public static List<SimpleObject> GenerateSimpleObjects(int count)
    {
        var objects = new List<SimpleObject>(count);

        for (int i = 0; i < count; i++)
        {
            objects.Add(new SimpleObject(
                Id: i,
                Name: $"Item {i}",
                Active: i % 2 == 0,
                Score: 0.5 + (i % 100) * 0.01,
                Category: Categories[i % Categories.Length]
            ));
        }

        return objects;
    }

    /// <summary>
    /// Generates a single complex object for small object benchmarks.
    /// </summary>
    public static object GenerateSingleComplexObject()
    {
        return new
        {
            id = 12345,
            name = "Test Object",
            description = "A sample object for benchmarking",
            active = true,
            score = 98.5,
            tags = new[] { "benchmark", "test", "performance" },
            metadata = new
            {
                created = "2024-01-15T10:30:00Z",
                modified = "2024-06-20T14:45:00Z",
                version = 3
            }
        };
    }

    /// <summary>
    /// Generates a wrapper object containing repositories for encoding.
    /// </summary>
    public static object GenerateRepositoriesWrapper(int count)
    {
        return new { repositories = GenerateGitHubRepositories(count) };
    }

    /// <summary>
    /// Generates a wrapper object containing employees for encoding.
    /// </summary>
    public static object GenerateEmployeesWrapper(int count)
    {
        return new { employees = GenerateEmployees(count) };
    }

    /// <summary>
    /// Generates a wrapper object containing analytics for encoding.
    /// </summary>
    public static object GenerateAnalyticsWrapper(int days)
    {
        return new { analytics = GenerateDailyAnalytics(days) };
    }

    /// <summary>
    /// Generates a wrapper object containing user profiles for encoding.
    /// </summary>
    public static object GenerateUserProfilesWrapper(int count)
    {
        return new { users = GenerateUserProfiles(count) };
    }

    // =====================================================
    // Official benchmark dataset generators
    // =====================================================

    /// <summary>
    /// Generates e-commerce orders with nested structures.
    /// Official benchmark: 50 orders, 33% tabular eligibility.
    /// </summary>
    public static List<EcommerceOrder> GenerateEcommerceOrders(int count)
    {
        var orders = new List<EcommerceOrder>(count);
        var random = new Random(42); // Fixed seed for reproducibility
        var statuses = new[] { "pending", "processing", "shipped", "delivered", "cancelled" };
        var shippingMethods = new[] { "standard", "express", "overnight", "pickup" };

        for (int i = 0; i < count; i++)
        {
            var itemCount = random.Next(1, 6);
            var items = new List<OrderItem>(itemCount);
            decimal total = 0;

            for (int j = 0; j < itemCount; j++)
            {
                var price = Math.Round((decimal)(random.NextDouble() * 100 + 10), 2);
                var qty = random.Next(1, 4);
                var subtotal = price * qty;
                total += subtotal;

                items.Add(new OrderItem(
                    ProductId: $"PROD-{i:D4}-{j:D2}",
                    Name: $"Product {i * 10 + j}",
                    Quantity: qty,
                    Price: price,
                    Subtotal: subtotal
                ));
            }

            var hasShipping = i % 3 != 0; // 2/3 have shipping info
            var orderDate = new DateTime(2024, 1, 1).AddDays(i).ToString("yyyy-MM-dd");

            orders.Add(new EcommerceOrder(
                OrderId: $"ORD-{i:D4}",
                Customer: new CustomerInfo(
                    Id: 1000 + i,
                    Name: $"Customer {i}",
                    Email: $"customer{i}@example.com",
                    Phone: $"+1-555-{1000 + i:D4}"
                ),
                Items: items,
                OrderDate: orderDate,
                Status: statuses[i % statuses.Length],
                Total: Math.Round(total, 2),
                Shipping: hasShipping ? new ShippingInfo(
                    Method: shippingMethods[i % shippingMethods.Length],
                    TrackingNumber: $"TRK{i:D8}",
                    EstimatedDelivery: new DateTime(2024, 1, 1).AddDays(i + 5).ToString("yyyy-MM-dd"),
                    Address: new AddressInfo(
                        Street: $"{100 + i} Shipping Street",
                        City: $"City{i % 20}",
                        State: "CA",
                        PostalCode: $"{90000 + i:D5}",
                        Country: "USA",
                        Coordinates: null
                    )
                ) : null
            ));
        }

        return orders;
    }

    /// <summary>
    /// Generates semi-uniform event logs.
    /// Official benchmark: 75 logs, 50% tabular eligibility (half have nested error objects).
    /// </summary>
    public static List<EventLog> GenerateEventLogs(int count)
    {
        var logs = new List<EventLog>(count);
        var levels = new[] { "DEBUG", "INFO", "WARN", "ERROR" };
        var sources = new[] { "api", "database", "cache", "auth", "scheduler" };
        var random = new Random(42);

        for (int i = 0; i < count; i++)
        {
            var level = levels[i % levels.Length];
            var hasError = level == "ERROR" || (level == "WARN" && i % 3 == 0);
            var timestamp = new DateTime(2024, 6, 1, 0, 0, 0).AddMinutes(i * 5).ToString("O");

            logs.Add(new EventLog(
                EventId: $"EVT-{i:D6}",
                Timestamp: timestamp,
                Level: level,
                Source: sources[i % sources.Length],
                Message: $"Event message {i}: {(hasError ? "Operation failed" : "Operation completed successfully")}",
                Error: hasError ? new EventErrorInfo(
                    Code: $"ERR_{1000 + i % 50}",
                    Message: $"Error details for event {i}",
                    StackTrace: i % 5 == 0 ? $"at Module.Function() line {i}\n   at Caller.Method() line {i + 10}" : null
                ) : null,
                Metadata: i % 4 == 0 ? new Dictionary<string, object>
                {
                    ["requestId"] = $"req-{i:D8}",
                    ["duration"] = random.Next(10, 5000),
                    ["retry"] = i % 2 == 0
                } : null
            ));
        }

        return logs;
    }

    /// <summary>
    /// Generates a deeply nested configuration object.
    /// Official benchmark: 1 config, 0% tabular eligibility.
    /// </summary>
    public static NestedConfig GenerateNestedConfig()
    {
        return new NestedConfig(
            Database: new DatabaseConfig(
                ConnectionString: "Server=db.example.com;Database=prod;User=app;Password=***",
                MaxConnections: 100,
                Timeout: 30000,
                Retry: new RetryConfig(MaxRetries: 3, DelayMs: 1000, ExponentialBackoff: true),
                Pool: new PoolConfig(MinSize: 10, MaxSize: 50, IdleTimeout: 300000)
            ),
            Cache: new CacheConfig(
                Provider: "redis",
                DefaultTtlSeconds: 3600,
                TtlOverrides: new Dictionary<string, int>
                {
                    ["user-session"] = 86400,
                    ["api-response"] = 300,
                    ["static-content"] = 604800
                },
                Redis: new RedisConfig(
                    Host: "redis.example.com",
                    Port: 6379,
                    Password: "***",
                    Database: 0
                )
            ),
            Logging: new LoggingConfig(
                Level: "INFO",
                Sinks: [
                    new LogSinkConfig("console", null, "json"),
                    new LogSinkConfig("file", "/var/log/app.log", "text"),
                    new LogSinkConfig("elasticsearch", null, null)
                ],
                CategoryLevels: new Dictionary<string, string>
                {
                    ["Microsoft"] = "WARNING",
                    ["System"] = "WARNING",
                    ["App.Database"] = "DEBUG"
                }
            ),
            Security: new SecurityConfig(
                Auth: new AuthConfig(
                    Provider: "oauth2",
                    TokenExpiryMinutes: 60,
                    AllowedIssuers: ["https://auth.example.com", "https://login.microsoftonline.com"]
                ),
                Cors: new CorsConfig(
                    AllowedOrigins: ["https://app.example.com", "https://admin.example.com"],
                    AllowedMethods: ["GET", "POST", "PUT", "DELETE"],
                    AllowCredentials: true
                ),
                RateLimit: new RateLimitConfig(
                    RequestsPerMinute: 100,
                    BurstLimit: 20,
                    ExemptPaths: ["/health", "/metrics"]
                )
            ),
            Features: new FeatureFlags(
                EnableNewUI: true,
                EnableBetaFeatures: false,
                PerUserFlags: new Dictionary<string, bool>
                {
                    ["user-123"] = true,
                    ["user-456"] = false
                },
                Rollout: new RolloutConfig(
                    Percentage: 25,
                    IncludedUsers: ["beta-tester-1", "beta-tester-2"],
                    ExcludedUsers: ["vip-user-1"]
                )
            )
        );
    }

    // =====================================================
    // Wrapper methods with official benchmark sizes
    // =====================================================

    /// <summary>Official: 50 e-commerce orders (nested, 33% tabular)</summary>
    public static object GenerateOfficialEcommerceOrders()
        => new { orders = GenerateEcommerceOrders(OfficialOrderCount) };

    /// <summary>Official: 75 event logs (semi-uniform, 50% tabular)</summary>
    public static object GenerateOfficialEventLogs()
        => new { events = GenerateEventLogs(OfficialEventLogCount) };

    /// <summary>Official: 1 nested config (deep, 0% tabular)</summary>
    public static object GenerateOfficialNestedConfig()
        => new { config = GenerateNestedConfig() };

    /// <summary>Official: 100 employees (uniform, 100% tabular)</summary>
    public static object GenerateOfficialEmployees()
        => GenerateEmployeesWrapper(OfficialEmployeeCount);

    /// <summary>Official: 60 days analytics (uniform, 100% tabular)</summary>
    public static object GenerateOfficialAnalytics()
        => GenerateAnalyticsWrapper(OfficialAnalyticsDays);

    /// <summary>Official: 100 GitHub repos (uniform, 100% tabular)</summary>
    public static object GenerateOfficialRepositories()
        => GenerateRepositoriesWrapper(OfficialRepoCount);

    /// <summary>Official: 50 user profiles (nested)</summary>
    public static object GenerateOfficialUserProfiles()
        => GenerateUserProfilesWrapper(OfficialUserProfileCount);
}
