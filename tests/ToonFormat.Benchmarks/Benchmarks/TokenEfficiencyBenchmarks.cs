using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Toon.Format;
using ToonFormat.Benchmarks.Data;

namespace ToonFormat.Benchmarks;

/// <summary>
/// Benchmarks comparing token efficiency between TOON and JSON formats.
/// Uses tiktoken o200k_base tokenizer (GPT-4o) for accurate token counting.
/// 
/// This mirrors the official TOON benchmark methodology:
/// - Same datasets (employees, analytics, repos, etc.)
/// - Same tokenizer (tiktoken o200k_base)
/// - Both byte and token metrics
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TokenEfficiencyBenchmarks
{
    // Official benchmark datasets
    private object _employees100 = null!;
    private object _analytics60 = null!;
    private object _repositories100 = null!;
    private object _userProfiles50 = null!;
    private object _ecommerceOrders50 = null!;
    private object _eventLogs75 = null!;
    private object _nestedConfig = null!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false // Minified JSON for fair comparison
    };

    [GlobalSetup]
    public void Setup()
    {
        // Use official benchmark sizes
        _employees100 = BenchmarkDataGenerator.GenerateOfficialEmployees();
        _analytics60 = BenchmarkDataGenerator.GenerateOfficialAnalytics();
        _repositories100 = BenchmarkDataGenerator.GenerateOfficialRepositories();
        _userProfiles50 = BenchmarkDataGenerator.GenerateOfficialUserProfiles();
        _ecommerceOrders50 = BenchmarkDataGenerator.GenerateOfficialEcommerceOrders();
        _eventLogs75 = BenchmarkDataGenerator.GenerateOfficialEventLogs();
        _nestedConfig = BenchmarkDataGenerator.GenerateOfficialNestedConfig();
    }

    // =====================
    // Employees (100 items) - 100% tabular
    // =====================

    [Benchmark(Baseline = true, Description = "Employees: JSON")]
    public string Employees_Json() => JsonSerializer.Serialize(_employees100, JsonOptions);

    [Benchmark(Description = "Employees: TOON")]
    public string Employees_Toon() => ToonEncoder.Encode(_employees100);

    [Benchmark(Description = "Employees: TOON+Tab")]
    public string Employees_ToonTab() => ToonEncoder.Encode(_employees100, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

    // =====================
    // Analytics (60 days) - 100% tabular
    // =====================

    [Benchmark(Description = "Analytics: JSON")]
    public string Analytics_Json() => JsonSerializer.Serialize(_analytics60, JsonOptions);

    [Benchmark(Description = "Analytics: TOON")]
    public string Analytics_Toon() => ToonEncoder.Encode(_analytics60);

    [Benchmark(Description = "Analytics: TOON+Tab")]
    public string Analytics_ToonTab() => ToonEncoder.Encode(_analytics60, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

    // =====================
    // GitHub Repos (100 items) - 100% tabular
    // =====================

    [Benchmark(Description = "Repos: JSON")]
    public string Repos_Json() => JsonSerializer.Serialize(_repositories100, JsonOptions);

    [Benchmark(Description = "Repos: TOON")]
    public string Repos_Toon() => ToonEncoder.Encode(_repositories100);

    [Benchmark(Description = "Repos: TOON+Tab")]
    public string Repos_ToonTab() => ToonEncoder.Encode(_repositories100, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

    // =====================
    // User Profiles (50 items) - Nested
    // =====================

    [Benchmark(Description = "Profiles: JSON")]
    public string Profiles_Json() => JsonSerializer.Serialize(_userProfiles50, JsonOptions);

    [Benchmark(Description = "Profiles: TOON")]
    public string Profiles_Toon() => ToonEncoder.Encode(_userProfiles50);

    [Benchmark(Description = "Profiles: TOON+Fold")]
    public string Profiles_ToonFolded() => ToonEncoder.Encode(_userProfiles50, new ToonEncodeOptions { KeyFolding = ToonKeyFolding.Safe });

    // =====================
    // E-commerce Orders (50 items) - 33% tabular
    // =====================

    [Benchmark(Description = "Orders: JSON")]
    public string Orders_Json() => JsonSerializer.Serialize(_ecommerceOrders50, JsonOptions);

    [Benchmark(Description = "Orders: TOON")]
    public string Orders_Toon() => ToonEncoder.Encode(_ecommerceOrders50);

    [Benchmark(Description = "Orders: TOON+Tab")]
    public string Orders_ToonTab() => ToonEncoder.Encode(_ecommerceOrders50, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

    // =====================
    // Event Logs (75 items) - 50% tabular
    // =====================

    [Benchmark(Description = "Events: JSON")]
    public string Events_Json() => JsonSerializer.Serialize(_eventLogs75, JsonOptions);

    [Benchmark(Description = "Events: TOON")]
    public string Events_Toon() => ToonEncoder.Encode(_eventLogs75);

    [Benchmark(Description = "Events: TOON+Tab")]
    public string Events_ToonTab() => ToonEncoder.Encode(_eventLogs75, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

    // =====================
    // Nested Config (1 deep config) - 0% tabular
    // =====================

    [Benchmark(Description = "Config: JSON")]
    public string Config_Json() => JsonSerializer.Serialize(_nestedConfig, JsonOptions);

    [Benchmark(Description = "Config: TOON")]
    public string Config_Toon() => ToonEncoder.Encode(_nestedConfig);

    [Benchmark(Description = "Config: TOON+Fold")]
    public string Config_ToonFolded() => ToonEncoder.Encode(_nestedConfig, new ToonEncodeOptions { KeyFolding = ToonKeyFolding.Safe });

    /// <summary>
    /// Prints a detailed summary of token efficiency using tiktoken.
    /// Call TokenAnalysis.RunAnalysis() for the full report.
    /// </summary>
    public static void PrintTokenEfficiencySummary()
    {
        TokenAnalysis.RunAnalysis();
    }
}
