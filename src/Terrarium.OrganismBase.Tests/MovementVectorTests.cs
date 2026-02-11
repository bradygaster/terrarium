using System.Drawing;
using OrganismBase;
using Xunit;

namespace Terrarium.OrganismBase.Tests;

public class MovementVectorTests
{
    [Fact]
    public void Constructor_ValidDestinationAndSpeed_SetsProperties()
    {
        var mv = new MovementVector(new Point(100, 200), 5);
        Assert.Equal(new Point(100, 200), mv.Destination);
        Assert.Equal(5, mv.Speed);
        Assert.False(mv.IsStopped);
    }

    [Fact]
    public void Constructor_SpeedBelowTwo_Throws()
    {
        Assert.Throws<ApplicationException>(() => new MovementVector(new Point(10, 10), 1));
    }

    [Fact]
    public void Constructor_SpeedOfZero_Throws()
    {
        Assert.Throws<ApplicationException>(() => new MovementVector(new Point(10, 10), 0));
    }

    [Fact]
    public void Constructor_NegativeSpeed_Throws()
    {
        Assert.Throws<ApplicationException>(() => new MovementVector(new Point(10, 10), -1));
    }

    [Fact]
    public void Constructor_EmptyDestinationNonZeroSpeed_Throws()
    {
        Assert.Throws<ApplicationException>(() => new MovementVector(Point.Empty, 5));
    }

    [Fact]
    public void Destination_ReturnsDefensiveCopy()
    {
        var mv = new MovementVector(new Point(100, 200), 5);
        var dest1 = mv.Destination;
        var dest2 = mv.Destination;
        Assert.Equal(dest1, dest2);
    }

    [Fact]
    public void ToString_ContainsSpeedAndCoordinates()
    {
        var mv = new MovementVector(new Point(50, 75), 10);
        var s = mv.ToString();
        Assert.Contains("50", s);
        Assert.Contains("75", s);
        Assert.Contains("10", s);
    }
}
