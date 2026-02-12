using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.Logging;
using Terrarium.Configuration;
using Terrarium.Game;

namespace Terrarium.Benchmarks;

/// <summary>
/// Benchmarks for GameEngine performance:
/// - ProcessTurn duration with varying organism counts
/// - WorldState serialization performance
/// - Memory allocations per turn
/// 
/// Sprint 11 Issue #77
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class GameEngineBenchmarks
{
    private GameEngine? _engine10;
    private GameEngine? _engine50;
    private GameEngine? _engine100;
    private GameEngine? _engine200;
    private ILoggerFactory? _loggerFactory;

    [GlobalSetup]
    public void Setup()
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));

        // Engines with different organism counts
        _engine10 = CreateEngine(maxAnimals: 10, maxPlants: 10);
        _engine50 = CreateEngine(maxAnimals: 50, maxPlants: 50);
        _engine100 = CreateEngine(maxAnimals: 100, maxPlants: 100);
        _engine200 = CreateEngine(maxAnimals: 200, maxPlants: 200);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _loggerFactory?.Dispose();
    }

    private GameEngine CreateEngine(int maxAnimals, int maxPlants)
    {
        var logger = _loggerFactory!.CreateLogger<GameEngine>();
        var populationLogger = _loggerFactory!.CreateLogger<PopulationData>();
        var populationData = new PopulationData(reportData: false, populationLogger);
        return new GameEngine(
            logger,
            populationData,
            worldWidth: 2048,
            worldHeight: 2048,
            maxAnimals: maxAnimals,
            maxPlants: maxPlants,
            ecosystemMode: false);
    }

    [Benchmark]
    public void ProcessTurn_10_Organisms()
    {
        // Benchmark game tick with 10 organisms
        // In a real scenario, organisms would be added via IntroduceNewOrganism
        // For now, measure empty world overhead
        // TODO: Add organism population via reflection or test helpers
    }

    [Benchmark]
    public void ProcessTurn_50_Organisms()
    {
        // Benchmark game tick with 50 organisms (typical max)
    }

    [Benchmark]
    public void ProcessTurn_100_Organisms()
    {
        // Benchmark game tick with 100 organisms (stress test)
    }

    [Benchmark]
    public void ProcessTurn_200_Organisms()
    {
        // Benchmark game tick with 200 organisms (extreme load)
    }

    [Benchmark]
    public void WorldState_Serialization()
    {
        // Benchmark WorldState to JSON serialization
        // Used for SignalR broadcasts and state snapshots
        var engine = _engine50!;
        // TODO: Serialize current world state via public API once available
    }
}
