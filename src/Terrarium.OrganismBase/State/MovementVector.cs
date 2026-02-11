// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Drawing;

namespace OrganismBase;

/// <summary>
/// Used to define a vector along which creatures can move.
/// Encompasses both destination and speed.
/// </summary>
public class MovementVector
{
    private readonly int speed;
    private Point destination;

    public MovementVector(Point destination, int speed)
    {
        if (speed < 2)
        {
            throw new ApplicationException("Speed must be positive and > 1.");
        }

        if (!destination.IsEmpty)
        {
            this.destination = new Point(destination.X, destination.Y);
        }
        else
        {
            if (speed != 0)
            {
                throw new ApplicationException("Speed must be zero if destination is empty");
            }
            this.destination = Point.Empty;
        }

        this.speed = speed;
    }

    public Point Destination
    {
        get
        {
            if (destination.IsEmpty)
                return Point.Empty;
            else
                return new Point(destination.X, destination.Y);
        }
    }

    public int Speed => speed;

    public Boolean IsStopped => destination.IsEmpty;

    public override string ToString()
    {
        return "MovementVector {" + destination.X + "," + destination.Y + " speed=" + speed + "}";
    }
}
