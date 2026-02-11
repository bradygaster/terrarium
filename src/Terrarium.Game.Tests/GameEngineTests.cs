using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OrganismBase;
using Xunit;

namespace Terrarium.Game.Tests;

/// <summary>
/// Tests for GameEngine: initialization, tick execution, and world vector management.
/// </summary>
public class GameEngineTests
{
    private static GameEngine CreateEngine(
        int worldWidth = 2048,
        int worldHeight = 2048,
        int maxAnimals = 50,
        int maxPlants = 50)
    {
        var logger = NullLogger<GameEngine>.Instance;
        var popLogger = NullLogger<PopulationData>.Instance;
        var populationData = new PopulationData(false, popLogger);

        return new GameEngine(logger, populationData,
            worldWidth, worldHeight, maxAnimals, maxPlants);
    }

    // --- Initialization ---

    [Fact]
    public void GameEngine_CanBeCreated()
    {
        var engine = CreateEngine();
        Assert.NotNull(engine);
    }

    [Fact]
    public void GameEngine_HasCorrectDimensions()
    {
        var engine = CreateEngine(2048, 2048);
        Assert.Equal(2048, engine.WorldWidth);
        Assert.Equal(2048, engine.WorldHeight);
    }

    [Fact]
    public void GameEngine_HasCorrectMaxCounts()
    {
        var engine = CreateEngine(maxAnimals: 100, maxPlants: 75);
        Assert.Equal(100, engine.MaxAnimals);
        Assert.Equal(75, engine.MaxPlants);
    }

    [Fact]
    public void GameEngine_StartsAtPhaseZero()
    {
        var engine = CreateEngine();
        Assert.Equal(0, engine.TurnPhase);
    }

    [Fact]
    public void GameEngine_StartsWithZeroCounts()
    {
        var engine = CreateEngine();
        Assert.Equal(0, engine.AnimalCount);
        Assert.Equal(0, engine.PlantCount);
    }

    [Fact]
    public void GameEngine_HasCurrentVector()
    {
        var engine = CreateEngine();
        Assert.NotNull(engine.CurrentVector);
    }

    [Fact]
    public void GameEngine_CurrentVector_HasImmutableState()
    {
        var engine = CreateEngine();
        Assert.True(engine.CurrentVector!.State.IsImmutable);
    }

    [Fact]
    public void GameEngine_CurrentVector_StartsAtTickZero()
    {
        var engine = CreateEngine();
        Assert.Equal(0, engine.CurrentVector!.State.TickNumber);
    }

    [Fact]
    public void GameEngine_HasPopulationData()
    {
        var engine = CreateEngine();
        Assert.NotNull(engine.PopulationData);
    }

    [Fact]
    public void GameEngine_DefaultNotEcosystemMode()
    {
        var engine = CreateEngine();
        Assert.False(engine.EcosystemMode);
    }

    // --- Grid dimensions ---

    [Fact]
    public void GameEngine_GridWidth_IsDerivedFromWorldWidth()
    {
        var engine = CreateEngine(2048, 2048);
        Assert.True(engine.GridWidth > 0);
        Assert.Equal(engine.WorldWidth >> EngineSettings.GridWidthPowerOfTwo, engine.GridWidth);
    }

    [Fact]
    public void GameEngine_GridHeight_IsDerivedFromWorldHeight()
    {
        var engine = CreateEngine(2048, 2048);
        Assert.True(engine.GridHeight > 0);
        Assert.Equal(engine.WorldHeight >> EngineSettings.GridHeightPowerOfTwo, engine.GridHeight);
    }

    // --- ProcessTurn ---

    [Fact]
    public void ProcessTurn_AdvancesPhase()
    {
        var engine = CreateEngine();
        Assert.Equal(0, engine.TurnPhase);

        var complete = engine.ProcessTurn();
        Assert.False(complete);
        Assert.Equal(1, engine.TurnPhase);
    }

    [Fact]
    public void ProcessTurn_TenCalls_CompletesOneTick()
    {
        var engine = CreateEngine();
        bool complete = false;

        for (int i = 0; i < 10; i++)
        {
            complete = engine.ProcessTurn();
        }

        Assert.True(complete);
        Assert.Equal(0, engine.TurnPhase); // resets after full tick
    }

    [Fact]
    public void ProcessTurn_AfterFullTick_TickNumberIncrements()
    {
        var engine = CreateEngine();
        Assert.Equal(0, engine.CurrentVector!.State.TickNumber);

        for (int i = 0; i < 10; i++)
            engine.ProcessTurn();

        Assert.Equal(1, engine.CurrentVector!.State.TickNumber);
    }

    [Fact]
    public void ProcessTurn_MultipleFullTicks_IncrementCorrectly()
    {
        var engine = CreateEngine();

        for (int tick = 0; tick < 3; tick++)
            for (int phase = 0; phase < 10; phase++)
                engine.ProcessTurn();

        Assert.Equal(3, engine.CurrentVector!.State.TickNumber);
    }

    [Fact]
    public void ProcessTurn_NineCallsDoNotComplete()
    {
        var engine = CreateEngine();
        bool complete = false;

        for (int i = 0; i < 9; i++)
            complete = engine.ProcessTurn();

        Assert.False(complete);
        Assert.Equal(9, engine.TurnPhase);
    }

    // --- WorldVectorChanged event ---

    [Fact]
    public void WorldVectorChanged_FiresAfterFullTick()
    {
        var engine = CreateEngine();
        var fired = false;
        engine.WorldVectorChanged += (_, _) => fired = true;

        for (int i = 0; i < 10; i++)
            engine.ProcessTurn();

        Assert.True(fired);
    }

    // --- StopGame ---

    [Fact]
    public void StopGame_ClearsPopulationData()
    {
        var engine = CreateEngine();
        engine.StopGame(false);
        Assert.Null(engine.PopulationData);
    }

    [Fact]
    public void StopGame_WithSerialize_DoesNotThrow()
    {
        var engine = CreateEngine();
        var ex = Record.Exception(() => engine.StopGame(true));
        Assert.Null(ex);
    }

    // --- World dimension normalization ---

    [Fact]
    public void GameEngine_NormalizesWorldDimensions_ToGridCellBoundaries()
    {
        // If dimensions aren't multiples of grid cell size, they get rounded up
        var engine = CreateEngine(2049, 2049);
        Assert.Equal(0, engine.WorldWidth % EngineSettings.GridCellWidth);
        Assert.Equal(0, engine.WorldHeight % EngineSettings.GridCellHeight);
    }
}
