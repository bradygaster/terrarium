using System.Drawing;
using OrganismBase;
using Xunit;

namespace Terrarium.OrganismBase.Tests;

public class VectorTests
{
    [Fact]
    public void Constructor_XY_SetsProperties()
    {
        var v = new Vector(3.0, 4.0);
        Assert.Equal(3.0, v.X);
        Assert.Equal(4.0, v.Y);
    }

    [Fact]
    public void Constructor_Point_SetsProperties()
    {
        var v = new Vector(new Point(5, 10));
        Assert.Equal(5.0, v.X);
        Assert.Equal(10.0, v.Y);
    }

    [Fact]
    public void Point_ReturnsIntegerCoordinates()
    {
        var v = new Vector(3.7, 4.2);
        Assert.Equal(new Point(3, 4), v.Point);
    }

    [Fact]
    public void TrueMagnitude_KnownVector_ReturnsCorrectValue()
    {
        var v = new Vector(3.0, 4.0);
        Assert.Equal(5.0, v.TrueMagnitude, 5);
    }

    [Fact]
    public void Magnitude_UsesApproximation()
    {
        var v = new Vector(3.0, 4.0);
        // FastMagnitude should be close to TrueMagnitude
        Assert.True(Math.Abs(v.Magnitude - v.TrueMagnitude) < v.TrueMagnitude * 0.15);
    }

    [Fact]
    public void Direction_PositiveX_ReturnsZero()
    {
        var v = new Vector(1.0, 0.0);
        Assert.Equal(0.0, v.Direction, 5);
    }

    [Fact]
    public void Direction_PositiveY_ReturnsHalfPi()
    {
        var v = new Vector(0.0, 1.0);
        Assert.Equal(Math.PI / 2, v.Direction, 5);
    }

    [Fact]
    public void Direction_NegativeX_ReturnsPi()
    {
        var v = new Vector(-1.0, 0.0);
        Assert.Equal(Math.PI, v.Direction, 5);
    }

    [Fact]
    public void Scale_MultipliesComponents()
    {
        var v = new Vector(3.0, 4.0);
        var scaled = v.Scale(2.0);
        Assert.Equal(6.0, scaled.X);
        Assert.Equal(8.0, scaled.Y);
    }

    [Fact]
    public void Rotate_90Degrees_RotatesCorrectly()
    {
        var v = new Vector(1.0, 0.0);
        var rotated = v.Rotate(Math.PI / 2);
        Assert.Equal(0.0, rotated.X, 5);
        Assert.Equal(1.0, rotated.Y, 5);
    }

    [Fact]
    public void GetUnitVector_ReturnsUnitLength()
    {
        var v = new Vector(3.0, 4.0);
        var unit = v.GetUnitVector();
        // Unit vector magnitude should be close to 1
        Assert.True(Math.Abs(unit.TrueMagnitude - 1.0) < 0.2);
    }

    [Fact]
    public void Subtract_ReturnsCorrectVector()
    {
        var result = Vector.Subtract(new Point(1, 2), new Point(4, 6));
        Assert.Equal(3.0, result.X);
        Assert.Equal(4.0, result.Y);
    }

    [Fact]
    public void Add_ReturnsCorrectPoint()
    {
        var result = Vector.Add(new Point(1, 2), new Vector(3.0, 4.0));
        Assert.Equal(new Point(4, 6), result);
    }

    [Fact]
    public void ToRadians_180Degrees_ReturnsPi()
    {
        Assert.Equal(Math.PI, Vector.ToRadians(180.0), 5);
    }

    [Fact]
    public void ToDegrees_Pi_Returns180()
    {
        Assert.Equal(180.0, Vector.ToDegrees(Math.PI), 5);
    }

    [Fact]
    public void ToString_ContainsMagnitude()
    {
        var v = new Vector(3.0, 4.0);
        var s = v.ToString();
        Assert.Contains("3", s);
        Assert.Contains("4", s);
        Assert.Contains("mag=", s);
    }
}
