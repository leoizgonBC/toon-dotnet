using BenchmarkDotNet.Running;
using ToonFormat.Benchmarks;

// =====================================================
// TOON Format Benchmarks for .NET
// 
// Mirrors the official TOON benchmark methodology:
// - Uses tiktoken o200k_base tokenizer (GPT-4o)
// - Same dataset sizes (employees:100, analytics:60, repos:100, etc.)
// - Both byte and token metrics
// =====================================================

if (args.Length > 0)
{
    var arg = args[0].ToLowerInvariant();

    // Quick token efficiency analysis (no BenchmarkDotNet overhead)
    if (arg is "analyze" or "analysis" or "tokens")
    {
        TokenAnalysis.RunAnalysis();
        return;
    }

    // Help
    if (arg is "help" or "-h" or "--help" or "/?")
    {
        PrintHelp();
        return;
    }
}

// Run BenchmarkDotNet benchmarks
Console.WriteLine("Running BenchmarkDotNet benchmarks...");
Console.WriteLine("Use 'analyze' argument for quick token efficiency analysis.");
Console.WriteLine();

BenchmarkSwitcher.FromAssembly(typeof(EncodeBenchmarks).Assembly)
    .Run(args);

static void PrintHelp()
{
    Console.WriteLine("""
    TOON Format Benchmarks for .NET
    ================================
    
    Usage:
      dotnet run -c Release [options]
    
    Options:
      analyze       Quick token efficiency analysis using tiktoken (no BenchmarkDotNet)
      --filter *    Run specific benchmarks (BenchmarkDotNet filter)
      --list tree   List available benchmarks
      --help        Show this help
    
    Examples:
      dotnet run -c Release                          # Run all benchmarks (interactive menu)
      dotnet run -c Release analyze                  # Quick token analysis
      dotnet run -c Release -- --filter *Encode*    # Run encode benchmarks only
      dotnet run -c Release -- --filter *Json*      # Run JSON comparison benchmarks
      dotnet run -c Release -- --list tree          # List all benchmarks
    
    Available Benchmark Classes:
      EncodeBenchmarks           - TOON encoding performance
      DecodeBenchmarks           - TOON decoding performance
      TokenEfficiencyBenchmarks  - TOON vs JSON token efficiency
      JsonComparisonBenchmarks   - Direct TOON vs JSON comparison (DOM vs DOM)
    
    Official Benchmark Dataset Sizes:
      - Employees:      100 records (uniform, 100% tabular)
      - Analytics:       60 days (uniform, 100% tabular)
      - GitHub Repos:   100 records (uniform, 100% tabular)
      - User Profiles:   50 records (nested)
      - E-commerce:      50 orders (33% tabular)
      - Event Logs:      75 entries (50% tabular)
      - Nested Config:    1 deep config (0% tabular)
    
    Token Analysis uses tiktoken o200k_base (GPT-4o tokenizer) for accurate token counting.
    """);
}
