using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Terrarium.Benchmarks;

/// <summary>
/// Main entry point for running Terrarium benchmarks.
/// Sprint 11 Issue #77 — Load and stress testing.
/// 
/// Run with: dotnet run -c Release --project src/Terrarium.Benchmarks
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<GameEngineBenchmarks>();
        // Uncomment to run SignalR benchmarks (requires server):
        // var signalRSummary = BenchmarkRunner.Run<SignalRBenchmarks>();
    }
}
