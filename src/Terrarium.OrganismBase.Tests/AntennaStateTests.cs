using OrganismBase;
using Xunit;

namespace Terrarium.OrganismBase.Tests;

public class AntennaStateTests
{
    [Fact]
    public void Constructor_NullState_DefaultsToLeft()
    {
        var state = new AntennaState((AntennaState?)null);
        Assert.Equal(AntennaPosition.Left, state.LeftAntenna);
        Assert.Equal(AntennaPosition.Left, state.RightAntenna);
    }

    [Fact]
    public void Constructor_WithPositions_SetsCorrectly()
    {
        var state = new AntennaState(AntennaPosition.Top, AntennaPosition.Bottom);
        Assert.Equal(AntennaPosition.Top, state.LeftAntenna);
        Assert.Equal(AntennaPosition.Bottom, state.RightAntenna);
    }

    [Fact]
    public void Constructor_CopiesFromExistingState()
    {
        var original = new AntennaState(AntennaPosition.Forward, AntennaPosition.Backward);
        var copy = new AntennaState(original);
        Assert.Equal(AntennaPosition.Forward, copy.LeftAntenna);
        Assert.Equal(AntennaPosition.Backward, copy.RightAntenna);
    }

    [Fact]
    public void LeftAntenna_CanSet_WhenMutable()
    {
        var state = new AntennaState((AntennaState?)null);
        state.LeftAntenna = AntennaPosition.UpperRight;
        Assert.Equal(AntennaPosition.UpperRight, state.LeftAntenna);
    }

    [Fact]
    public void LeftAntenna_CannotSet_WhenImmutable()
    {
        var state = new AntennaState((AntennaState?)null);
        state.MakeImmutable();
        state.LeftAntenna = AntennaPosition.Top;
        // Stays at default because immutable silently ignores
        Assert.Equal(AntennaPosition.Left, state.LeftAntenna);
    }

    [Fact]
    public void AntennaValue_EncodesLeftAndRight()
    {
        var state = new AntennaState(AntennaPosition.Top, AntennaPosition.Bottom);
        // Top=2, Bottom=3 => 2*10 + 3 = 23
        Assert.Equal((int)AntennaPosition.Top * 10 + (int)AntennaPosition.Bottom, state.AntennaValue);
    }

    [Fact]
    public void AntennaValue_CanSet_WhenMutable()
    {
        var state = new AntennaState((AntennaState?)null);
        state.AntennaValue = 23; // Left=2 (Top), Right=3 (Bottom)
        Assert.Equal(AntennaPosition.Top, state.LeftAntenna);
        Assert.Equal(AntennaPosition.Bottom, state.RightAntenna);
    }

    [Fact]
    public void AntennaValue_CannotSet_WhenImmutable()
    {
        var state = new AntennaState((AntennaState?)null);
        state.MakeImmutable();
        state.AntennaValue = 23;
        Assert.Equal(AntennaPosition.Left, state.LeftAntenna);
    }

    [Fact]
    public void AntennaValue_OutOfRange_DoesNotSet()
    {
        var state = new AntennaState((AntennaState?)null);
        state.AntennaValue = 100; // Out of range (>= 100)
        Assert.Equal(AntennaPosition.Left, state.LeftAntenna);
    }

    [Fact]
    public void AntennaValue_Negative_DoesNotSet()
    {
        var state = new AntennaState((AntennaState?)null);
        state.AntennaValue = -1;
        Assert.Equal(AntennaPosition.Left, state.LeftAntenna);
    }
}
