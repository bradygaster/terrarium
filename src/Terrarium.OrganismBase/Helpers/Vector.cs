// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Drawing;

namespace OrganismBase;

/// <summary>
/// A classic algebraic vector class for complex movement algorithms.
/// </summary>
public class Vector
{
    public Vector(double x, double y)
    {
        X = x;
        Y = y;
    }

    public Vector(Point point)
    {
        X = point.X;
        Y = point.Y;
    }

    public double X { get; private set; }

    public double Y { get; private set; }

    public Point Point => new Point((int)X, (int)Y);

    public double Magnitude => FastMagnitude;

    public double TrueMagnitude => Math.Sqrt((X * X) + (Y * Y));

    public double Direction
    {
        get
        {
            var direction = Math.Atan2(Y, X);
            if (direction < 0)
            {
                direction = 2 * Math.PI + direction;
            }
            return direction;
        }
    }

    internal double FastMagnitude
    {
        get
        {
            var absX = (X < 0 ? -X : X);
            var absY = (Y < 0 ? -Y : Y);

            if (absX < absY)
                return absX + absY - absX / 2;

            return absX + absY - absY / 2;
        }
    }

    public Vector Scale(double scalar) => new Vector(X * scalar, Y * scalar);

    public static double ToRadians(double degrees) => (degrees / 360) * 2 * Math.PI;

    public static double ToDegrees(double radians) => (radians / (2 * Math.PI)) * 360;

    public Vector Rotate(double radians)
    {
        return new Vector(Math.Cos(radians) * X - Math.Sin(radians) * Y,
                          Math.Sin(radians) * X + Math.Cos(radians) * Y);
    }

    public Vector GetUnitVector()
    {
        var magnitude = Magnitude;
        return new Vector(X / magnitude, Y / magnitude);
    }

    public static Vector Subtract(Point point1, Point point2)
    {
        return new Vector(point2.X - point1.X, point2.Y - point1.Y);
    }

    public static Point Add(Point point, Vector vector)
    {
        return new Point(point.X + (int)vector.X, point.Y + (int)vector.Y);
    }

    public override string ToString()
    {
        return string.Format("{{{0}, {1}, mag={2}}}", X, Y, Magnitude);
    }
}
