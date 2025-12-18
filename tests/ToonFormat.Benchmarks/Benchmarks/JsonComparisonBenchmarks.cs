using System.Text.Json;
using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Toon.Format;
using ToonFormat.Benchmarks.Data;

namespace ToonFormat.Benchmarks;

/// <summary>
/// Direct comparison benchmarks between TOON and System.Text.Json.
/// 
/// IMPORTANT: This compares like-for-like operations:
/// - Decode: Both produce DOM (JsonNode.Parse vs ToonDecoder.Decode)
/// - Encode: Object to string serialization
/// 
/// This mirrors the official TOON benchmark methodology where
/// both formats are decoded to equivalent in-memory representations.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class JsonComparisonBenchmarks
{
    // Official benchmark datasets
    private object _employees100 = null!;
    private object _userProfiles50 = null!;
    private object _ecommerceOrders50 = null!;
    private object _nestedConfig = null!;

    // Pre-serialized strings for decode benchmarks
    private string _employeesJson = null!;
    private string _employeesToon = null!;
    private string _profilesJson = null!;
    private string _profilesToon = null!;
    private string _ordersJson = null!;
    private string _ordersToon = null!;
    private string _configJson = null!;
    private string _configToon = null!;

    private static readonly JsonSerializerOptions CompactJsonOptions = new()
    {
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true
    };

    [GlobalSetup]
    public void Setup()
    {
        // Use official benchmark sizes
        _employees100 = BenchmarkDataGenerator.GenerateOfficialEmployees();
        _userProfiles50 = BenchmarkDataGenerator.GenerateOfficialUserProfiles();
        _ecommerceOrders50 = BenchmarkDataGenerator.GenerateOfficialEcommerceOrders();
        _nestedConfig = BenchmarkDataGenerator.GenerateOfficialNestedConfig();

        // Pre-serialize for decode benchmarks
        _employeesJson = JsonSerializer.Serialize(_employees100, CompactJsonOptions);
        _employeesToon = ToonEncoder.Encode(_employees100);

        _profilesJson = JsonSerializer.Serialize(_userProfiles50, CompactJsonOptions);
        _profilesToon = ToonEncoder.Encode(_userProfiles50);

        _ordersJson = JsonSerializer.Serialize(_ecommerceOrders50, CompactJsonOptions);
        _ordersToon = ToonEncoder.Encode(_ecommerceOrders50);

        _configJson = JsonSerializer.Serialize(_nestedConfig, CompactJsonOptions);
        _configToon = ToonEncoder.Encode(_nestedConfig);
    }

    // =====================================================
    // ENCODE COMPARISON - Employees (100% tabular)
    // =====================================================

    [Benchmark(Baseline = true, Description = "Encode Employees: JSON")]
    public string Encode_Employees_Json()
        => JsonSerializer.Serialize(_employees100, CompactJsonOptions);

    [Benchmark(Description = "Encode Employees: JSON Indented")]
    public string Encode_Employees_JsonIndented()
        => JsonSerializer.Serialize(_employees100, IndentedJsonOptions);

    [Benchmark(Description = "Encode Employees: TOON")]
    public string Encode_Employees_Toon()
        => ToonEncoder.Encode(_employees100);

    [Benchmark(Description = "Encode Employees: TOON+Tab")]
    public string Encode_Employees_ToonTab()
        => ToonEncoder.Encode(_employees100, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

    // =====================================================
    // ENCODE COMPARISON - User Profiles (Nested)
    // =====================================================

    [Benchmark(Description = "Encode Profiles: JSON")]
    public string Encode_Profiles_Json()
        => JsonSerializer.Serialize(_userProfiles50, CompactJsonOptions);

    [Benchmark(Description = "Encode Profiles: TOON")]
    public string Encode_Profiles_Toon()
        => ToonEncoder.Encode(_userProfiles50);

    [Benchmark(Description = "Encode Profiles: TOON+Fold")]
    public string Encode_Profiles_ToonFolded()
        => ToonEncoder.Encode(_userProfiles50, new ToonEncodeOptions { KeyFolding = ToonKeyFolding.Safe });

    // =====================================================
    // ENCODE COMPARISON - E-commerce Orders (33% tabular)
    // =====================================================

    [Benchmark(Description = "Encode Orders: JSON")]
    public string Encode_Orders_Json()
        => JsonSerializer.Serialize(_ecommerceOrders50, CompactJsonOptions);

    [Benchmark(Description = "Encode Orders: TOON")]
    public string Encode_Orders_Toon()
        => ToonEncoder.Encode(_ecommerceOrders50);

    // =====================================================
    // ENCODE COMPARISON - Nested Config (0% tabular)
    // =====================================================

    [Benchmark(Description = "Encode Config: JSON")]
    public string Encode_Config_Json()
        => JsonSerializer.Serialize(_nestedConfig, CompactJsonOptions);

    [Benchmark(Description = "Encode Config: TOON")]
    public string Encode_Config_Toon()
        => ToonEncoder.Encode(_nestedConfig);

    // =====================================================
    // DECODE COMPARISON - DOM vs DOM (fair comparison!)
    // JsonNode.Parse produces JsonNode, ToonDecoder produces dict/list
    // Both are in-memory DOM representations.
    // =====================================================

    [Benchmark(Description = "Decode Employees: JSON→DOM")]
    public JsonNode? Decode_Employees_JsonDom()
        => JsonNode.Parse(_employeesJson);

    [Benchmark(Description = "Decode Employees: TOON→DOM")]
    public object? Decode_Employees_ToonDom()
        => ToonDecoder.Decode(_employeesToon);

    [Benchmark(Description = "Decode Profiles: JSON→DOM")]
    public JsonNode? Decode_Profiles_JsonDom()
        => JsonNode.Parse(_profilesJson);

    [Benchmark(Description = "Decode Profiles: TOON→DOM")]
    public object? Decode_Profiles_ToonDom()
        => ToonDecoder.Decode(_profilesToon);

    [Benchmark(Description = "Decode Orders: JSON→DOM")]
    public JsonNode? Decode_Orders_JsonDom()
        => JsonNode.Parse(_ordersJson);

    [Benchmark(Description = "Decode Orders: TOON→DOM")]
    public object? Decode_Orders_ToonDom()
        => ToonDecoder.Decode(_ordersToon);

    [Benchmark(Description = "Decode Config: JSON→DOM")]
    public JsonNode? Decode_Config_JsonDom()
        => JsonNode.Parse(_configJson);

    [Benchmark(Description = "Decode Config: TOON→DOM")]
    public object? Decode_Config_ToonDom()
        => ToonDecoder.Decode(_configToon);

    // =====================================================
    // ROUNDTRIP COMPARISON
    // =====================================================

    [Benchmark(Description = "Roundtrip Employees: JSON")]
    public JsonNode? Roundtrip_Employees_Json()
    {
        var json = JsonSerializer.Serialize(_employees100, CompactJsonOptions);
        return JsonNode.Parse(json);
    }

    [Benchmark(Description = "Roundtrip Employees: TOON")]
    public object? Roundtrip_Employees_Toon()
    {
        var toon = ToonEncoder.Encode(_employees100);
        return ToonDecoder.Decode(toon);
    }

    [Benchmark(Description = "Roundtrip Orders: JSON")]
    public JsonNode? Roundtrip_Orders_Json()
    {
        var json = JsonSerializer.Serialize(_ecommerceOrders50, CompactJsonOptions);
        return JsonNode.Parse(json);
    }

    [Benchmark(Description = "Roundtrip Orders: TOON")]
    public object? Roundtrip_Orders_Toon()
    {
        var toon = ToonEncoder.Encode(_ecommerceOrders50);
        return ToonDecoder.Decode(toon);
    }
}
