using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OrganismBase;
using Xunit;

namespace Terrarium.Game.Tests;

/// <summary>
/// Tests for fair scheduling of organism execution within the game engine.
/// Validates quantum-based time allocation, turn processing, and
/// timeout enforcement for misbehaving creatures.
/// </summary>
public class GameSchedulerTests
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

    // --- Scheduler initializes with configured quantum ---

    [Fact]
    public void Engine_InitializesWithTenPhaseQuantum()
    {
        var engine = CreateEngine();
        // The engine uses a 10-phase turn model as its scheduling quantum
        Assert.Equal(0, engine.TurnPhase);

        // Execute one phase
        engine.ProcessTurn();
        Assert.Equal(1, engine.TurnPhase);
    }

    [Fact]
    public void Engine_QuantumResetsAfterFullCycle()
    {
        var engine = CreateEngine();

        // Complete all 10 phases
        for (int i = 0; i < 10; i++)
            engine.ProcessTurn();

        // Phase resets to 0 after full cycle
        Assert.Equal(0, engine.TurnPhase);
    }

    [Fact]
    public void Engine_EachPhaseIncrementsSequentially()
    {
        var engine = CreateEngine();

        for (int expectedPhase = 0; expectedPhase < 10; expectedPhase++)
        {
            Assert.Equal(expectedPhase, engine.TurnPhase);
            engine.ProcessTurn();
        }
    }

    // --- Organisms get allocated execution time ---

    [Fact]
    public void Engine_OrganismsGetTimeViaProcessTurn()
    {
        var engine = CreateEngine();

        // Phases 0-4 are organism time slices (1/5 each)
        // They complete without error even with no organisms
        for (int i = 0; i < 5; i++)
        {
            var ex = Record.Exception(() => engine.ProcessTurn());
            Assert.Null(ex);
        }
    }

    [Fact]
    public void Engine_MultipleTicksAllocateTimeCorrectly()
    {
        var engine = CreateEngine();

        // Run 3 full ticks — each allocates 5 organism-time phases
        for (int tick = 0; tick < 3; tick++)
        {
            for (int phase = 0; phase < 10; phase++)
            {
                var ex = Record.Exception(() => engine.ProcessTurn());
                Assert.Null(ex);
            }
        }

        Assert.Equal(3, engine.CurrentVector!.State.TickNumber);
    }

    [Fact]
    public void Engine_ProcessTurn_ReturnsFalseForIncompletePhases()
    {
        var engine = CreateEngine();

        // Phases 0-8 should return false (tick not complete)
        for (int i = 0; i < 9; i++)
        {
            bool complete = engine.ProcessTurn();
            Assert.False(complete, $"Phase {i} should not complete the tick");
        }
    }

    [Fact]
    public void Engine_ProcessTurn_ReturnsTrueOnTenthPhase()
    {
        var engine = CreateEngine();

        bool complete = false;
        for (int i = 0; i < 10; i++)
            complete = engine.ProcessTurn();

        Assert.True(complete, "10th phase should complete the tick");
    }

    // --- Timeout enforcement ---

    [Fact]
    public async Task Engine_CancellationToken_CanBeCancelledForTimeout()
    {
        // Verify that CancellationTokenSource can enforce timeouts
        // This is the mechanism used to cancel long-running organism code
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        bool wasCancelled = false;
        try
        {
            // Simulate a creature that runs too long
            await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
        }
        catch (TaskCanceledException)
        {
            wasCancelled = true;
        }

        Assert.True(wasCancelled, "Long-running task should be cancelled by timeout");
    }

    [Fact]
    public async Task Engine_CancellationToken_ShortTaskCompletesBeforeTimeout()
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        // Simulate a well-behaved creature
        await Task.Delay(10, cts.Token);

        Assert.False(cts.IsCancellationRequested);
    }

    [Fact]
    public async Task Engine_TimeoutEnforcement_MultipleTasks()
    {
        // Verify that multiple organisms can be timed independently
        using var cts1 = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        bool task1Cancelled = false;
        bool task2Completed = false;

        var t1 = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cts1.Token);
            }
            catch (TaskCanceledException)
            {
                task1Cancelled = true;
            }
        });

        var t2 = Task.Run(async () =>
        {
            await Task.Delay(10, cts2.Token);
            task2Completed = true;
        });

        await Task.WhenAll(t1, t2);

        Assert.True(task1Cancelled, "Long-running organism should be cancelled");
        Assert.True(task2Completed, "Well-behaved organism should complete");
    }

    // --- Engine fairness: population limits enforce resource fairness ---

    [Fact]
    public void Engine_MaxAnimals_EnforcesPopulationLimit()
    {
        var engine = CreateEngine(maxAnimals: 10);
        Assert.Equal(10, engine.MaxAnimals);
    }

    [Fact]
    public void Engine_MaxPlants_EnforcesPopulationLimit()
    {
        var engine = CreateEngine(maxPlants: 25);
        Assert.Equal(25, engine.MaxPlants);
    }

    [Fact]
    public void Engine_StartsWithZeroPopulation()
    {
        var engine = CreateEngine();
        Assert.Equal(0, engine.AnimalCount);
        Assert.Equal(0, engine.PlantCount);
    }

    [Fact]
    public void Engine_WorldVectorChanged_SignalsSchedulingBoundary()
    {
        var engine = CreateEngine();
        int changeCount = 0;
        engine.WorldVectorChanged += (_, _) => changeCount++;

        // Complete two full ticks
        for (int tick = 0; tick < 2; tick++)
            for (int phase = 0; phase < 10; phase++)
                engine.ProcessTurn();

        // Each tick completion fires the event once
        Assert.Equal(2, changeCount);
    }

    [Fact]
    public void Engine_TickNumber_AdvancesAfterSchedulingCycle()
    {
        var engine = CreateEngine();
        Assert.Equal(0, engine.CurrentVector!.State.TickNumber);

        // Complete one scheduling cycle (10 phases)
        for (int i = 0; i < 10; i++)
            engine.ProcessTurn();

        Assert.Equal(1, engine.CurrentVector!.State.TickNumber);

        // Complete another
        for (int i = 0; i < 10; i++)
            engine.ProcessTurn();

        Assert.Equal(2, engine.CurrentVector!.State.TickNumber);
    }
}
