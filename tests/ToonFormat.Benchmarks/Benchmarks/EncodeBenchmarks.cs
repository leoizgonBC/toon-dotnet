using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Toon.Format;
using ToonFormat.Benchmarks.Data;

namespace ToonFormat.Benchmarks;

/// <summary>
/// Benchmarks for TOON encoding operations.
/// Measures throughput and memory allocation for various data structures.
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
public class EncodeBenchmarks
{
    // Official benchmark datasets
    private object _employees100 = null!;
    private object _analytics60 = null!;
    private object _repos100 = null!;
    private object _profiles50 = null!;
    private object _orders50 = null!;
    private object _events75 = null!;
    private object _nestedConfig = null!;

    // Extra sizes for scalability testing
    private object _repos1000 = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Official benchmark sizes
        _employees100 = BenchmarkDataGenerator.GenerateOfficialEmployees();
        _analytics60 = BenchmarkDataGenerator.GenerateOfficialAnalytics();
        _repos100 = BenchmarkDataGenerator.GenerateOfficialRepositories();
        _profiles50 = BenchmarkDataGenerator.GenerateOfficialUserProfiles();
        _orders50 = BenchmarkDataGenerator.GenerateOfficialEcommerceOrders();
        _events75 = BenchmarkDataGenerator.GenerateOfficialEventLogs();
        _nestedConfig = BenchmarkDataGenerator.GenerateOfficialNestedConfig();

        // Extra for scalability
        _repos1000 = BenchmarkDataGenerator.GenerateRepositoriesWrapper(1000);
    }

    // =====================================================
    // UNIFORM TABULAR DATA (100% tabular eligibility)
    // =====================================================

    [Benchmark(Description = "Employees (100)")]
    public string Encode_Employees()
        => ToonEncoder.Encode(_employees100);

    [Benchmark(Description = "Employees (100) + Tab")]
    public string Encode_Employees_Tab()
        => ToonEncoder.Encode(_employees100, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

    [Benchmark(Description = "Analytics (60 days)")]
    public string Encode_Analytics()
        => ToonEncoder.Encode(_analytics60);

    [Benchmark(Description = "Analytics (60 days) + Tab")]
    public string Encode_Analytics_Tab()
        => ToonEncoder.Encode(_analytics60, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

    [Benchmark(Description = "Repos (100)")]
    public string Encode_Repos()
        => ToonEncoder.Encode(_repos100);

    [Benchmark(Description = "Repos (100) + Tab")]
    public string Encode_Repos_Tab()
        => ToonEncoder.Encode(_repos100, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

    // =====================================================
    // NESTED / MIXED DATA
    // =====================================================

    [Benchmark(Description = "Profiles (50, nested)")]
    public string Encode_Profiles()
        => ToonEncoder.Encode(_profiles50);

    [Benchmark(Description = "Profiles (50) + Fold")]
    public string Encode_Profiles_Fold()
        => ToonEncoder.Encode(_profiles50, new ToonEncodeOptions { KeyFolding = ToonKeyFolding.Safe });

    [Benchmark(Description = "Orders (50, 33% tabular)")]
    public string Encode_Orders()
        => ToonEncoder.Encode(_orders50);

    [Benchmark(Description = "Orders (50) + Tab")]
    public string Encode_Orders_Tab()
        => ToonEncoder.Encode(_orders50, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

    [Benchmark(Description = "Events (75, 50% tabular)")]
    public string Encode_Events()
        => ToonEncoder.Encode(_events75);

    [Benchmark(Description = "Events (75) + Tab")]
    public string Encode_Events_Tab()
        => ToonEncoder.Encode(_events75, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

    // =====================================================
    // DEEP NESTED (0% tabular)
    // =====================================================

    [Benchmark(Description = "Config (deep nested)")]
    public string Encode_Config()
        => ToonEncoder.Encode(_nestedConfig);

    [Benchmark(Description = "Config + Fold")]
    public string Encode_Config_Fold()
        => ToonEncoder.Encode(_nestedConfig, new ToonEncodeOptions { KeyFolding = ToonKeyFolding.Safe });

    // =====================================================
    // SCALABILITY
    // =====================================================

    [Benchmark(Description = "Repos (1000, scalability)")]
    public string Encode_Repos_1000()
        => ToonEncoder.Encode(_repos1000);

    [Benchmark(Description = "Repos (1000) + Tab")]
    public string Encode_Repos_1000_Tab()
        => ToonEncoder.Encode(_repos1000, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });
}
