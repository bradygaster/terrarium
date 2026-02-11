// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Drawing;
using OrganismBase;

namespace Terrarium.Game;

/// <summary>
/// A moving teleportation zone in the Terrarium world.
/// </summary>
public class TeleportZone
{
    private readonly int _teleportID;
    private Rectangle _rectangle;
    private MovementVector? _vector;

    public TeleportZone(Rectangle rectangle, MovementVector? vector, int id)
    { _rectangle = rectangle; _vector = vector; _teleportID = id; }

    public Rectangle Rectangle => new(_rectangle.Left, _rectangle.Top, _rectangle.Width, _rectangle.Height);
    public int ID => _teleportID;
    public MovementVector? Vector => _vector;

    public TeleportZone Clone() => new(_rectangle, _vector, ID);

    public TeleportZone SetRectangle(Rectangle rectangle)
    { var z = Clone(); z._rectangle = rectangle; return z; }

    public TeleportZone SetVector(MovementVector? vector)
    { var z = Clone(); z._vector = vector; return z; }

    public bool Contains(OrganismState state)
    {
        var d = _rectangle.Left - (state.Position.X - state.Radius);
        if (d < 0) { if (-d > _rectangle.Width + 1) return false; }
        else { if (d > (state.Radius * 2) + 1) return false; }
        d = _rectangle.Top - (state.Position.Y - state.Radius);
        if (d < 0) { if (-d > _rectangle.Height + 1) return false; }
        else { if (d > (state.Radius * 2) + 1) return false; }
        return true;
    }
}
