using OrganismBase;
using Xunit;

namespace Terrarium.OrganismBase.Tests;

public class ExceptionTests
{
    [Fact]
    public void GameEngineException_HasMessage()
    {
        var ex = new GameEngineException("test message");
        Assert.Equal("test message", ex.Message);
    }

    [Fact]
    public void TooManyPointsOnOneCharacteristicException_HasMessage()
    {
        var ex = new TooManyPointsOnOneCharacteristicException();
        Assert.Contains("100", ex.Message);
    }

    [Fact]
    public void SizeOutOfRangeCharacteristicException_ContainsBounds()
    {
        var ex = new SizeOutOfRangeCharacteristicException();
        Assert.Contains(EngineSettings.MaxMatureSize.ToString(), ex.Message);
        Assert.Contains(EngineSettings.MinMatureSize.ToString(), ex.Message);
    }

    [Fact]
    public void TooManyPointsOnOneCharacteristicException_IsGameEngineException()
    {
        var ex = new TooManyPointsOnOneCharacteristicException();
        Assert.IsAssignableFrom<GameEngineException>(ex);
    }

    [Fact]
    public void SizeOutOfRangeCharacteristicException_IsGameEngineException()
    {
        var ex = new SizeOutOfRangeCharacteristicException();
        Assert.IsAssignableFrom<GameEngineException>(ex);
    }
}
