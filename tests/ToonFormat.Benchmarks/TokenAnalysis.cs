using System.Text;
using System.Text.Json;
using TiktokenSharp;
using Toon.Format;
using ToonFormat.Benchmarks.Data;

namespace ToonFormat.Benchmarks;

/// <summary>
/// Token efficiency analysis using real tiktoken tokenizer (o200k_base).
/// This mirrors the official TOON benchmark methodology exactly.
/// 
/// Official benchmark uses:
/// - tiktoken with o200k_base encoding (GPT-4o tokenizer)
/// - Both bytes and tokens for comparison
/// - Multiple dataset types with varying tabular eligibility
/// </summary>
public static class TokenAnalysis
{
    private static TikToken? _tokenizer;

    private static readonly JsonSerializerOptions CompactJsonOptions = new()
    {
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions PrettyJsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Gets or initializes the tiktoken tokenizer with o200k_base encoding.
    /// This is the same encoding used by GPT-4o.
    /// </summary>
    private static TikToken GetTokenizer()
    {
        return _tokenizer ??= TikToken.GetEncoding("o200k_base");
    }

    /// <summary>
    /// Counts tokens using tiktoken o200k_base encoding.
    /// </summary>
    public static int CountTokens(string text)
    {
        return GetTokenizer().Encode(text).Count;
    }

    /// <summary>
    /// Runs the complete token efficiency analysis matching the official TOON benchmark.
    /// </summary>
    public static void RunAnalysis()
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                              TOKEN EFFICIENCY ANALYSIS: TOON vs JSON                                 ║");
        Console.WriteLine("║                         Using tiktoken o200k_base (GPT-4o tokenizer)                                 ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Official benchmark datasets
        var testCases = new (string Name, object Data, string TabularEligibility)[]
        {
            ("Employees (100)", BenchmarkDataGenerator.GenerateOfficialEmployees(), "100% (uniform)"),
            ("Analytics (60 days)", BenchmarkDataGenerator.GenerateOfficialAnalytics(), "100% (uniform)"),
            ("GitHub Repos (100)", BenchmarkDataGenerator.GenerateOfficialRepositories(), "100% (uniform)"),
            ("User Profiles (50)", BenchmarkDataGenerator.GenerateOfficialUserProfiles(), "Nested"),
            ("E-commerce Orders (50)", BenchmarkDataGenerator.GenerateOfficialEcommerceOrders(), "33% (nested)"),
            ("Event Logs (75)", BenchmarkDataGenerator.GenerateOfficialEventLogs(), "50% (semi-uniform)"),
            ("Nested Config", BenchmarkDataGenerator.GenerateOfficialNestedConfig(), "0% (deep)")
        };

        Console.WriteLine("Initializing tiktoken tokenizer (may download encoding on first run)...");
        _ = GetTokenizer();
        Console.WriteLine("Tokenizer ready.\n");

        // =====================================================
        // SECTION 1: Byte comparison (JSON compact vs TOON)
        // =====================================================
        Console.WriteLine("┌─────────────────────────────────────────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│                                    BYTE COMPARISON                                                   │");
        Console.WriteLine("├─────────────────────────────────────────────────────────────────────────────────────────────────────┤");
        Console.WriteLine(string.Format("│ {0,-25} {1,12} {2,12} {3,12} {4,12} {5,10} │", "Dataset", "JSON", "JSON+Tab", "TOON", "TOON+Tab", "Savings"));
        Console.WriteLine("├─────────────────────────────────────────────────────────────────────────────────────────────────────┤");

        var totalJsonBytes = 0L;
        var totalToonBytes = 0L;
        var totalToonTabBytes = 0L;

        foreach (var (name, data, _) in testCases)
        {
            var jsonCompact = JsonSerializer.Serialize(data, CompactJsonOptions);
            var jsonTabbed = jsonCompact.Replace("    ", "\t"); // Compact JSON doesn't have tabs, but for consistency
            var toon = ToonEncoder.Encode(data);
            var toonTab = ToonEncoder.Encode(data, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

            var jsonBytes = Encoding.UTF8.GetByteCount(jsonCompact);
            var jsonTabBytes = Encoding.UTF8.GetByteCount(jsonTabbed);
            var toonBytes = Encoding.UTF8.GetByteCount(toon);
            var toonTabBytes = Encoding.UTF8.GetByteCount(toonTab);

            totalJsonBytes += jsonBytes;
            totalToonBytes += toonBytes;
            totalToonTabBytes += toonTabBytes;

            var savings = (1.0 - (double)toonTabBytes / jsonBytes) * 100;

            Console.WriteLine($"│ {name,-25} {jsonBytes,12:N0} {jsonTabBytes,12:N0} {toonBytes,12:N0} {toonTabBytes,12:N0} {savings,9:F1}% │");
        }

        var totalSavingsBytes = (1.0 - (double)totalToonTabBytes / totalJsonBytes) * 100;
        Console.WriteLine("├─────────────────────────────────────────────────────────────────────────────────────────────────────┤");
        Console.WriteLine($"│ {"TOTAL",-25} {totalJsonBytes,12:N0} {"-",12} {totalToonBytes,12:N0} {totalToonTabBytes,12:N0} {totalSavingsBytes,9:F1}% │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();

        // =====================================================
        // SECTION 2: Token comparison (the main metric!)
        // =====================================================
        Console.WriteLine("┌─────────────────────────────────────────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│                              TOKEN COMPARISON (tiktoken o200k_base)                                  │");
        Console.WriteLine("├─────────────────────────────────────────────────────────────────────────────────────────────────────┤");
        Console.WriteLine(string.Format("│ {0,-25} {1,12} {2,12} {3,12} {4,12} {5,10} │", "Dataset", "JSON", "JSON+Tab", "TOON", "TOON+Tab", "Savings"));
        Console.WriteLine("├─────────────────────────────────────────────────────────────────────────────────────────────────────┤");

        var totalJsonTokens = 0;
        var totalToonTokens = 0;
        var totalToonTabTokens = 0;

        foreach (var (name, data, _) in testCases)
        {
            var jsonCompact = JsonSerializer.Serialize(data, CompactJsonOptions);
            var jsonTabbed = jsonCompact.Replace("    ", "\t");
            var toon = ToonEncoder.Encode(data);
            var toonTab = ToonEncoder.Encode(data, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

            var jsonTokens = CountTokens(jsonCompact);
            var jsonTabTokens = CountTokens(jsonTabbed);
            var toonTokensCount = CountTokens(toon);
            var toonTabTokens = CountTokens(toonTab);

            totalJsonTokens += jsonTokens;
            totalToonTokens += toonTokensCount;
            totalToonTabTokens += toonTabTokens;

            var savings = (1.0 - (double)toonTabTokens / jsonTokens) * 100;

            Console.WriteLine($"│ {name,-25} {jsonTokens,12:N0} {jsonTabTokens,12:N0} {toonTokensCount,12:N0} {toonTabTokens,12:N0} {savings,9:F1}% │");
        }

        var totalSavingsTokens = (1.0 - (double)totalToonTabTokens / totalJsonTokens) * 100;
        Console.WriteLine("├─────────────────────────────────────────────────────────────────────────────────────────────────────┤");
        Console.WriteLine($"│ {"TOTAL",-25} {totalJsonTokens,12:N0} {"-",12} {totalToonTokens,12:N0} {totalToonTabTokens,12:N0} {totalSavingsTokens,9:F1}% │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();

        // =====================================================
        // SECTION 3: Detailed breakdown by tabular eligibility
        // =====================================================
        Console.WriteLine("┌─────────────────────────────────────────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│                               DETAILED BREAKDOWN BY DATA TYPE                                        │");
        Console.WriteLine("├─────────────────────────────────────────────────────────────────────────────────────────────────────┤");
        Console.WriteLine(string.Format("│ {0,-25} {1,-18} {2,12} {3,12} {4,10} {5,10} │", "Dataset", "Tabular", "JSON Tok", "TOON Tok", "Savings", "Bytes/Tok"));
        Console.WriteLine("├─────────────────────────────────────────────────────────────────────────────────────────────────────┤");

        foreach (var (name, data, tabular) in testCases)
        {
            var json = JsonSerializer.Serialize(data, CompactJsonOptions);
            var toon = ToonEncoder.Encode(data, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

            var jsonTokens = CountTokens(json);
            var toonTokens = CountTokens(toon);
            var savings = (1.0 - (double)toonTokens / jsonTokens) * 100;

            var toonBytes = Encoding.UTF8.GetByteCount(toon);
            var bytesPerToken = (double)toonBytes / toonTokens;

            Console.WriteLine($"│ {name,-25} {tabular,-18} {jsonTokens,12:N0} {toonTokens,12:N0} {savings,9:F1}% {bytesPerToken,9:F2} │");
        }

        Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();

        // =====================================================
        // SECTION 4: Quick performance comparison
        // =====================================================
        Console.WriteLine("┌─────────────────────────────────────────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│                               QUICK PERFORMANCE TEST                                                 │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────────────────────────────┘");

        var employees = BenchmarkDataGenerator.GenerateOfficialEmployees();
        var employeesJson = JsonSerializer.Serialize(employees, CompactJsonOptions);
        var employeesToon = ToonEncoder.Encode(employees);

        const int iterations = 1000;

        // Warm up
        for (int i = 0; i < 100; i++)
        {
            _ = JsonSerializer.Serialize(employees, CompactJsonOptions);
            _ = ToonEncoder.Encode(employees);
            _ = JsonSerializer.Deserialize<object>(employeesJson);
            _ = ToonDecoder.Decode(employeesToon);
        }

        // JSON Encode
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            _ = JsonSerializer.Serialize(employees, CompactJsonOptions);
        }
        sw.Stop();
        var jsonEncodeMs = sw.Elapsed.TotalMilliseconds;

        // TOON Encode
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            _ = ToonEncoder.Encode(employees);
        }
        sw.Stop();
        var toonEncodeMs = sw.Elapsed.TotalMilliseconds;

        // JSON Decode
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            _ = JsonSerializer.Deserialize<object>(employeesJson);
        }
        sw.Stop();
        var jsonDecodeMs = sw.Elapsed.TotalMilliseconds;

        // TOON Decode
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            _ = ToonDecoder.Decode(employeesToon);
        }
        sw.Stop();
        var toonDecodeMs = sw.Elapsed.TotalMilliseconds;

        Console.WriteLine();
        Console.WriteLine($"  Employees (100 records) - {iterations} iterations:");
        Console.WriteLine($"  ┌──────────────┬────────────────┬────────────────┐");
        Console.WriteLine($"  │ Operation    │      Time (ms) │      Ops/sec   │");
        Console.WriteLine($"  ├──────────────┼────────────────┼────────────────┤");
        Console.WriteLine($"  │ JSON Encode  │ {jsonEncodeMs,14:F2} │ {iterations * 1000 / jsonEncodeMs,14:N0} │");
        Console.WriteLine($"  │ TOON Encode  │ {toonEncodeMs,14:F2} │ {iterations * 1000 / toonEncodeMs,14:N0} │");
        Console.WriteLine($"  │ JSON Decode  │ {jsonDecodeMs,14:F2} │ {iterations * 1000 / jsonDecodeMs,14:N0} │");
        Console.WriteLine($"  │ TOON Decode  │ {toonDecodeMs,14:F2} │ {iterations * 1000 / toonDecodeMs,14:N0} │");
        Console.WriteLine($"  └──────────────┴────────────────┴────────────────┘");
        Console.WriteLine();
        Console.WriteLine($"  Encode Ratio: TOON is {jsonEncodeMs / toonEncodeMs:F2}x vs JSON");
        Console.WriteLine($"  Decode Ratio: TOON is {jsonDecodeMs / toonDecodeMs:F2}x vs JSON");
        Console.WriteLine();

        // =====================================================
        // SECTION 5: Summary
        // =====================================================
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                                          SUMMARY                                                     ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║  • Average byte savings:  {totalSavingsBytes:F1}% (TOON+TAB vs JSON compact)                                          ║");
        Console.WriteLine($"║  • Average token savings: {totalSavingsTokens:F1}% (TOON+TAB vs JSON compact)                                          ║");
        Console.WriteLine($"║  • Tokenizer: tiktoken o200k_base (GPT-4o)                                                           ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════════════════════════════╝");
    }

    /// <summary>
    /// Runs a simplified analysis for a specific dataset and returns the results.
    /// </summary>
    public static TokenAnalysisResult AnalyzeDataset(string name, object data)
    {
        var jsonCompact = JsonSerializer.Serialize(data, CompactJsonOptions);
        var toon = ToonEncoder.Encode(data);
        var toonTab = ToonEncoder.Encode(data, new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB });

        return new TokenAnalysisResult
        {
            DatasetName = name,
            JsonBytes = Encoding.UTF8.GetByteCount(jsonCompact),
            ToonBytes = Encoding.UTF8.GetByteCount(toon),
            ToonTabBytes = Encoding.UTF8.GetByteCount(toonTab),
            JsonTokens = CountTokens(jsonCompact),
            ToonTokens = CountTokens(toon),
            ToonTabTokens = CountTokens(toonTab)
        };
    }
}

/// <summary>
/// Results from token analysis of a single dataset.
/// </summary>
public record TokenAnalysisResult
{
    public required string DatasetName { get; init; }
    public required int JsonBytes { get; init; }
    public required int ToonBytes { get; init; }
    public required int ToonTabBytes { get; init; }
    public required int JsonTokens { get; init; }
    public required int ToonTokens { get; init; }
    public required int ToonTabTokens { get; init; }

    public double ByteSavingsPercent => (1.0 - (double)ToonTabBytes / JsonBytes) * 100;
    public double TokenSavingsPercent => (1.0 - (double)ToonTabTokens / JsonTokens) * 100;
}
