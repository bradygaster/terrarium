// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Diagnostics;
using System.Drawing;
using OrganismBase;

namespace Terrarium.Game;

/// <summary>
/// Tracks a segment of a creature's movement path through a single grid cell.
/// </summary>
internal class MovementSegment
{
    private bool _active;
    private int _cellsLeftToResolve;

    internal MovementSegment(
        MovementSegment? previous, OrganismState state,
        Point startingPoint, int entryTime, int gridX, int gridY)
    {
        Debug.Assert((previous == null && entryTime == 0) || (previous != null && entryTime != 0));
        State = state;
        StartingPoint = startingPoint;
        EntryTime = entryTime;
        GridX = gridX;
        GridY = gridY;
        Previous = previous;
    }

    public int CellsLeftToResolve
    {
        get => _cellsLeftToResolve;
        set
        {
            if (value == 0)
            {
                if (!IsClipped)
                {
                    Active = true;
                    if (Previous != null)
                    {
                        Debug.Assert(Previous.Active);
                        Previous.Active = false;
                    }
                }
            }
            else if (value < 0) value = 0;
            _cellsLeftToResolve = value;
        }
    }

    public bool IsResolved
    {
        get => CellsLeftToResolve == 0;
        set
        {
            if (value) CellsLeftToResolve = 0;
            else throw new InvalidOperationException("Should never get unresolved");
        }
    }

    public Point StartingPoint { get; set; }
    public Point EndingPoint { get; set; }
    public int GridX { get; set; }
    public int GridY { get; set; }
    public int EntryTime { get; set; }
    public int ExitTime { get; set; }
    public OrganismState State { get; set; }
    public MovementSegment? Previous { get; set; }
    public MovementSegment? Next { get; set; }
    public OrganismState? BlockedByState { get; set; }

    internal bool Active
    {
        get => _active;
        set
        {
            Debug.Assert((value && !IsClipped) | !value);
            _active = value;
        }
    }

    internal bool IsStationarySegment
        => StartingPoint == EndingPoint && EntryTime == 0 && ExitTime == 0;

    internal bool IsClipped
        => EntryTime != 0 && Previous?.Next == null;

    internal bool IsStartingSegment => EntryTime == 0;

    public override string ToString()
        => $"{GridX}, {GridY}EntryTime={EntryTime}ExitTime={ExitTime}StartingPoint={StartingPoint}EndingPoint={EndingPoint}";

    public void ClipSegment(OrganismState blocker)
    {
        MovementSegment? segment = this;
        Debug.Assert(segment.Previous != null);
        segment.Previous!.BlockedByState = blocker;
        while (segment != null)
        {
            Debug.Assert(segment.Previous != null);
            segment.Previous!.Next = null;
            segment.CellsLeftToResolve = 0;
            segment = segment.Next;
        }
    }
}
