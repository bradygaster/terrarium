using System;
using System.Drawing;
using OrganismBase;
using Xunit;

namespace Terrarium.Game.Tests;

/// <summary>
/// Tests Point, MovementVector, and Vector types from OrganismBase
/// for movement calculations the game engine depends on.
/// </summary>
public class PointTests
{
    // --- Point basics ---

    [Fact]
    public void Point_DefaultIsEmpty()
    {
        var p = new Point();
        Assert.True(p.IsEmpty);
    }

    [Fact]
    public void Point_ConstructorSetsXY()
    {
        var p = new Point(10, 20);
        Assert.Equal(10, p.X);
        Assert.Equal(20, p.Y);
    }

    [Fact]
    public void Point_Equality()
    {
        var p1 = new Point(5, 10);
        var p2 = new Point(5, 10);
        Assert.Equal(p1, p2);
    }

    [Fact]
    public void Point_Inequality()
    {
        var p1 = new Point(5, 10);
        var p2 = new Point(10, 5);
        Assert.NotEqual(p1, p2);
    }

    // --- Rectangle basics ---

    [Fact]
    public void Rectangle_ConstructorSetsProperties()
    {
        var r = new Rectangle(10, 20, 100, 200);
        Assert.Equal(10, r.X);
        Assert.Equal(20, r.Y);
        Assert.Equal(100, r.Width);
        Assert.Equal(200, r.Height);
    }

    [Fact]
    public void Rectangle_Contains_PointInside()
    {
        var r = new Rectangle(0, 0, 100, 100);
        Assert.True(r.Contains(50, 50));
    }

    [Fact]
    public void Rectangle_Contains_PointOutside()
    {
        var r = new Rectangle(0, 0, 100, 100);
        Assert.False(r.Contains(150, 150));
    }

    [Fact]
    public void Rectangle_ContainsPoint()
    {
        var r = new Rectangle(0, 0, 100, 100);
        Assert.True(r.Contains(new Point(50, 50)));
        Assert.False(r.Contains(new Point(200, 200)));
    }

    [Fact]
    public void Rectangle_IntersectsWith_Overlapping()
    {
        var r1 = new Rectangle(0, 0, 100, 100);
        var r2 = new Rectangle(50, 50, 100, 100);
        Assert.True(r1.IntersectsWith(r2));
    }

    [Fact]
    public void Rectangle_IntersectsWith_NonOverlapping()
    {
        var r1 = new Rectangle(0, 0, 50, 50);
        var r2 = new Rectangle(100, 100, 50, 50);
        Assert.False(r1.IntersectsWith(r2));
    }

    // --- MovementVector ---

    [Fact]
    public void MovementVector_ConstructorSetsDestinationAndSpeed()
    {
        var dest = new Point(100, 200);
        var mv = new MovementVector(dest, 5);
        Assert.Equal(dest, mv.Destination);
        Assert.Equal(5, mv.Speed);
    }

    [Fact]
    public void MovementVector_IsNotStopped_WhenHasDestination()
    {
        var mv = new MovementVector(new Point(100, 200), 5);
        Assert.False(mv.IsStopped);
    }

    [Fact]
    public void MovementVector_SpeedMustBeGreaterThanOne()
    {
        Assert.Throws<ApplicationException>(() => new MovementVector(new Point(10, 20), 1));
        Assert.Throws<ApplicationException>(() => new MovementVector(new Point(10, 20), 0));
    }

    [Fact]
    public void MovementVector_ToString_ContainsCoordinates()
    {
        var mv = new MovementVector(new Point(42, 84), 3);
        var str = mv.ToString();
        Assert.Contains("42", str);
        Assert.Contains("84", str);
    }

    [Fact]
    public void MovementVector_Destination_ReturnsCopy()
    {
        var mv = new MovementVector(new Point(10, 20), 5);
        var d1 = mv.Destination;
        var d2 = mv.Destination;
        Assert.Equal(d1, d2);
    }

    // --- Vector ---

    [Fact]
    public void Vector_ConstructorFromDoubles()
    {
        var v = new Vector(3.0, 4.0);
        Assert.Equal(3.0, v.X);
        Assert.Equal(4.0, v.Y);
    }

    [Fact]
    public void Vector_ConstructorFromPoint()
    {
        var v = new Vector(new Point(5, 10));
        Assert.Equal(5.0, v.X);
        Assert.Equal(10.0, v.Y);
    }

    [Fact]
    public void Vector_TrueMagnitude_IsEuclidean()
    {
        var v = new Vector(3.0, 4.0);
        Assert.Equal(5.0, v.TrueMagnitude, 5);
    }

    [Fact]
    public void Vector_Magnitude_IsApproximatelyCorrect()
    {
        var v = new Vector(3.0, 4.0);
        // FastMagnitude is an approximation; should be in reasonable range
        Assert.InRange(v.Magnitude, 4.0, 6.0);
    }

    [Fact]
    public void Vector_Scale_MultipliesBothComponents()
    {
        var v = new Vector(2.0, 3.0);
        var scaled = v.Scale(2.0);
        Assert.Equal(4.0, scaled.X);
        Assert.Equal(6.0, scaled.Y);
    }

    [Fact]
    public void Vector_GetUnitVector_HasMagnitudeOfApproxOne()
    {
        var v = new Vector(3.0, 4.0);
        var unit = v.GetUnitVector();
        Assert.InRange(unit.Magnitude, 0.8, 1.2);
    }

    [Fact]
    public void Vector_Subtract_ComputesDifference()
    {
        var p1 = new Point(10, 20);
        var p2 = new Point(30, 50);
        var result = Vector.Subtract(p1, p2);
        Assert.Equal(20.0, result.X);
        Assert.Equal(30.0, result.Y);
    }

    [Fact]
    public void Vector_Add_CombinesPointAndVector()
    {
        var p = new Point(10, 20);
        var v = new Vector(5.0, 10.0);
        var result = Vector.Add(p, v);
        Assert.Equal(15, result.X);
        Assert.Equal(30, result.Y);
    }

    [Fact]
    public void Vector_Point_TruncatesToInt()
    {
        var v = new Vector(3.7, 4.9);
        var p = v.Point;
        Assert.Equal(3, p.X);
        Assert.Equal(4, p.Y);
    }

    [Fact]
    public void Vector_Direction_RightVector_IsZero()
    {
        var v = new Vector(1.0, 0.0);
        Assert.Equal(0.0, v.Direction, 5);
    }

    [Fact]
    public void Vector_Direction_UpVector_IsHalfPi()
    {
        var v = new Vector(0.0, 1.0);
        Assert.Equal(Math.PI / 2, v.Direction, 5);
    }

    [Fact]
    public void Vector_ToRadians_And_ToDegrees_AreInverse()
    {
        double degrees = 90.0;
        double radians = Vector.ToRadians(degrees);
        double back = Vector.ToDegrees(radians);
        Assert.Equal(degrees, back, 5);
    }

    [Fact]
    public void Vector_Rotate_90Degrees()
    {
        var v = new Vector(1.0, 0.0);
        var rotated = v.Rotate(Math.PI / 2);
        Assert.Equal(0.0, rotated.X, 5);
        Assert.Equal(1.0, rotated.Y, 5);
    }

    [Fact]
    public void Vector_ToString_ContainsComponents()
    {
        var v = new Vector(3.0, 4.0);
        var str = v.ToString();
        Assert.Contains("3", str);
        Assert.Contains("4", str);
        Assert.Contains("mag=", str);
    }
}
