using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Terrarium.Game;
using Xunit;

namespace Terrarium.Smoke.Tests;

/// <summary>
/// Smoke tests verifying the GameEngine initializes and can run at least one tick.
/// </summary>
public class GameEngineSmokeTests
{
    private static GameEngine CreateEngine()
    {
        var logger = NullLogger<GameEngine>.Instance;
        var popLogger = NullLogger<PopulationData>.Instance;
        var populationData = new PopulationData(false, popLogger);
        return new GameEngine(logger, populationData);
    }

    [Fact]
    public void GameEngine_Initializes_Without_Throwing()
    {
        var exception = Record.Exception(() => CreateEngine());

        Assert.Null(exception);
    }

    [Fact]
    public void GameEngine_Has_Valid_WorldVector_After_Init()
    {
        var engine = CreateEngine();

        Assert.NotNull(engine.CurrentVector);
        Assert.NotNull(engine.CurrentVector.State);
        Assert.True(engine.CurrentVector.State.IsImmutable);
    }

    [Fact]
    public void GameEngine_Runs_One_Full_Tick()
    {
        var engine = CreateEngine();
        bool completed = false;

        for (int phase = 0; phase < 10; phase++)
        {
            completed = engine.ProcessTurn();
        }

        Assert.True(completed, "GameEngine did not complete a full tick after 10 phases");
        Assert.Equal(1, engine.CurrentVector!.State.TickNumber);
    }

    [Fact]
    public void GameEngine_Runs_Multiple_Ticks_Without_Crashing()
    {
        var engine = CreateEngine();

        var exception = Record.Exception(() =>
        {
            for (int tick = 0; tick < 5; tick++)
                for (int phase = 0; phase < 10; phase++)
                    engine.ProcessTurn();
        });

        Assert.Null(exception);
        Assert.Equal(5, engine.CurrentVector!.State.TickNumber);
    }

    [Fact]
    public void GameEngine_WorldVectorChanged_Fires_On_Tick()
    {
        var engine = CreateEngine();
        bool eventFired = false;
        engine.WorldVectorChanged += (_, _) => eventFired = true;

        for (int phase = 0; phase < 10; phase++)
            engine.ProcessTurn();

        Assert.True(eventFired, "WorldVectorChanged event did not fire after a complete tick");
    }

    [Fact]
    public void GameEngine_StopGame_Does_Not_Throw()
    {
        var engine = CreateEngine();

        var exception = Record.Exception(() => engine.StopGame(false));

        Assert.Null(exception);
    }
}
