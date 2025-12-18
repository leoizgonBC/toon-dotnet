using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Toon.Format;
using ToonFormat.Benchmarks.Data;

namespace ToonFormat.Benchmarks;

/// <summary>
/// Benchmarks for TOON decoding operations.
/// Measures throughput and memory allocation for parsing various TOON structures.
/// 
/// Uses official benchmark sizes:
/// - Employees: 100 (uniform, 100% tabular)
/// - Analytics: 60 days (uniform, 100% tabular)
/// - Repos: 100 (uniform, 100% tabular)
/// - User Profiles: 50 (nested)
/// - E-commerce Orders: 50 (33% tabular)
/// - Event Logs: 75 (50% tabular)
/// - Nested Config: 1 (0% tabular)
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class DecodeBenchmarks
{
    // Pre-encoded TOON strings (official sizes)
    private string _employeesToon = null!;
    private string _employeesToonTab = null!;
    private string _analyticsToon = null!;
    private string _analyticsToonTab = null!;
    private string _reposToon = null!;
    private string _reposToonTab = null!;
    private string _profilesToon = null!;
    private string _profilesToonFold = null!;
    private string _ordersToon = null!;
    private string _ordersToonTab = null!;
    private string _eventsToon = null!;
    private string _eventsToonTab = null!;
    private string _configToon = null!;
    private string _configToonFold = null!;

    // Extra for scalability
    private string _repos1000Toon = null!;
    private string _repos1000ToonTab = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Generate and encode official datasets
        var employees = BenchmarkDataGenerator.GenerateOfficialEmployees();
        _employeesToon = ToonEncoder.Encode(employees);
        _employeesToonTab = ToonEncoder.Encode(employees, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

        var analytics = BenchmarkDataGenerator.GenerateOfficialAnalytics();
        _analyticsToon = ToonEncoder.Encode(analytics);
        _analyticsToonTab = ToonEncoder.Encode(analytics, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

        var repos = BenchmarkDataGenerator.GenerateOfficialRepositories();
        _reposToon = ToonEncoder.Encode(repos);
        _reposToonTab = ToonEncoder.Encode(repos, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

        var profiles = BenchmarkDataGenerator.GenerateOfficialUserProfiles();
        _profilesToon = ToonEncoder.Encode(profiles);
        _profilesToonFold = ToonEncoder.Encode(profiles, new ToonEncodeOptions { KeyFolding = ToonKeyFolding.Safe });

        var orders = BenchmarkDataGenerator.GenerateOfficialEcommerceOrders();
        _ordersToon = ToonEncoder.Encode(orders);
        _ordersToonTab = ToonEncoder.Encode(orders, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

        var events = BenchmarkDataGenerator.GenerateOfficialEventLogs();
        _eventsToon = ToonEncoder.Encode(events);
        _eventsToonTab = ToonEncoder.Encode(events, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

        var config = BenchmarkDataGenerator.GenerateOfficialNestedConfig();
        _configToon = ToonEncoder.Encode(config);
        _configToonFold = ToonEncoder.Encode(config, new ToonEncodeOptions { KeyFolding = ToonKeyFolding.Safe });

        // Scalability
        var repos1000 = BenchmarkDataGenerator.GenerateRepositoriesWrapper(1000);
        _repos1000Toon = ToonEncoder.Encode(repos1000);
        _repos1000ToonTab = ToonEncoder.Encode(repos1000, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });
    }

    // =====================================================
    // UNIFORM TABULAR DATA (100% tabular eligibility)
    // =====================================================

    [Benchmark(Description = "Employees (100)")]
    public object? Decode_Employees()
        => ToonDecoder.Decode(_employeesToon);

    [Benchmark(Description = "Employees (100) Tab")]
    public object? Decode_Employees_Tab()
        => ToonDecoder.Decode(_employeesToonTab);

    [Benchmark(Description = "Analytics (60 days)")]
    public object? Decode_Analytics()
        => ToonDecoder.Decode(_analyticsToon);

    [Benchmark(Description = "Analytics (60 days) Tab")]
    public object? Decode_Analytics_Tab()
        => ToonDecoder.Decode(_analyticsToonTab);

    [Benchmark(Description = "Repos (100)")]
    public object? Decode_Repos()
        => ToonDecoder.Decode(_reposToon);

    [Benchmark(Description = "Repos (100) Tab")]
    public object? Decode_Repos_Tab()
        => ToonDecoder.Decode(_reposToonTab);

    // =====================================================
    // NESTED / MIXED DATA
    // =====================================================

    [Benchmark(Description = "Profiles (50, nested)")]
    public object? Decode_Profiles()
        => ToonDecoder.Decode(_profilesToon);

    [Benchmark(Description = "Profiles (50) Folded")]
    public object? Decode_Profiles_Fold()
        => ToonDecoder.Decode(_profilesToonFold);

    [Benchmark(Description = "Profiles + PathExpansion")]
    public object? Decode_Profiles_PathExpand()
        => ToonDecoder.Decode(_profilesToonFold, new ToonDecodeOptions { ExpandPaths = ToonExpandPaths.Safe });

    [Benchmark(Description = "Orders (50, 33% tabular)")]
    public object? Decode_Orders()
        => ToonDecoder.Decode(_ordersToon);

    [Benchmark(Description = "Orders (50) Tab")]
    public object? Decode_Orders_Tab()
        => ToonDecoder.Decode(_ordersToonTab);

    [Benchmark(Description = "Events (75, 50% tabular)")]
    public object? Decode_Events()
        => ToonDecoder.Decode(_eventsToon);

    [Benchmark(Description = "Events (75) Tab")]
    public object? Decode_Events_Tab()
        => ToonDecoder.Decode(_eventsToonTab);

    // =====================================================
    // DEEP NESTED (0% tabular)
    // =====================================================

    [Benchmark(Description = "Config (deep nested)")]
    public object? Decode_Config()
        => ToonDecoder.Decode(_configToon);

    [Benchmark(Description = "Config Folded")]
    public object? Decode_Config_Fold()
        => ToonDecoder.Decode(_configToonFold);

    // =====================================================
    // SCALABILITY
    // =====================================================

    [Benchmark(Description = "Repos (1000)")]
    public object? Decode_Repos_1000()
        => ToonDecoder.Decode(_repos1000Toon);

    [Benchmark(Description = "Repos (1000) Tab")]
    public object? Decode_Repos_1000_Tab()
        => ToonDecoder.Decode(_repos1000ToonTab);

    // =====================================================
    // OPTIONS COMPARISON
    // =====================================================

    [Benchmark(Description = "Employees + Strict")]
    public object? Decode_Employees_Strict()
        => ToonDecoder.Decode(_employeesToon, new ToonDecodeOptions { Strict = true });
}
