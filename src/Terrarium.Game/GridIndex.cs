// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Diagnostics;
using System.Drawing;
using OrganismBase;

namespace Terrarium.Game;

/// <summary>
/// Implements the movement/collision algorithm for the Terrarium.
/// Breaks creature paths into cell-based MovementSegments, then resolves
/// conflicts when multiple creatures try to occupy the same cell.
/// </summary>
internal class GridIndex
{
    private const int TimeWindow = 10000;
    private readonly Dictionary<int, List<SegmentWrapper>> _gridSquares = new();
    private readonly List<SegmentWrapper> _sortedList = new(300);

    public GridIndex()
    {
        StartSegments = new List<MovementSegment>(300);
    }

    public List<MovementSegment> StartSegments { get; private set; }

    internal void ResolvePaths()
    {
        _sortedList.Sort(new SegmentWrapperComparer());
        foreach (var wrapper in _sortedList)
        {
            var segment = wrapper.Segment;
            var list = wrapper.ParentList;
            Debug.Assert(list.Count >= 1);
            if (segment.IsResolved)
            {
                Debug.Assert(segment.IsStartingSegment || segment.IsClipped);
                continue;
            }
            if (segment.IsStartingSegment) { segment.CellsLeftToResolve = 0; }
            else if (list.Count == 1) { segment.CellsLeftToResolve--; continue; }
            else
            {
                foreach (var tw in list)
                {
                    var ts = tw.Segment;
                    if (ts.State == segment.State) continue;
                    if (!ts.Active) continue;
                    segment.ClipSegment(ts.State);
                    break;
                }
                segment.CellsLeftToResolve--;
            }
        }
    }

    public void AddPath(OrganismState state, Point p1, Point p2, int gridWidth, int gridHeight)
    {
        var x0 = p1.X; var y0 = p1.Y; var x1 = p2.X; var y1 = p2.Y;
        var dy = y1 - y0; var dx = x1 - x0;
        int stepx, stepy; var timeslice = 0;
        Debug.Assert(x0 > -1 && x1 > -1 && y0 > -1 && y1 > -1);
        if (dy < 0) { dy = -dy; stepy = -1; } else { stepy = 1; }
        if (dx < 0) { dx = -dx; stepx = -1; } else { stepx = 1; }
        dy <<= 1; dx <<= 1;
        var gridX = x0 >> EngineSettings.GridWidthPowerOfTwo;
        var gridY = y0 >> EngineSettings.GridWidthPowerOfTwo;
        Debug.Assert(gridX == state.GridX && gridY == state.GridY);
        var segment = new MovementSegment(null, state, new Point(x0, y0), 0, gridX, gridY)
            { EndingPoint = new Point(p1.X, p1.Y) };
        AddSegment(segment, gridWidth, gridHeight);
        if (p1 == p2) return;

        if (dx > dy)
        {
            if ((x1 - x0) != 0)
            {
                Debug.Assert((x1 - x0) < TimeWindow);
                timeslice = TimeWindow / ((x1 - x0) * stepx);
                Debug.Assert(timeslice != 0);
            }
            var fraction = dy - (dx >> 1);
            while (x0 != x1)
            {
                if (fraction >= 0) { y0 += stepy; fraction -= dx; }
                x0 += stepx; fraction += dy;
                gridX = x0 >> EngineSettings.GridWidthPowerOfTwo;
                gridY = y0 >> EngineSettings.GridHeightPowerOfTwo;
                segment.ExitTime += timeslice;
                if (gridX != segment.GridX || gridY != segment.GridY)
                {
                    var last = segment;
                    segment = new MovementSegment(last, state, new Point(x0, y0), last.ExitTime, gridX, gridY)
                        { ExitTime = last.ExitTime };
                    last.Next = segment;
                    AddSegment(segment, gridWidth, gridHeight);
                }
                segment.EndingPoint = new Point(x0, y0);
            }
        }
        else
        {
            if ((y1 - y0) != 0)
            {
                Debug.Assert((y1 - y0) < TimeWindow);
                timeslice = TimeWindow / ((y1 - y0) * stepy);
                Debug.Assert(timeslice != 0);
            }
            var fraction = dx - (dy >> 1);
            while (y0 != y1)
            {
                if (fraction >= 0) { x0 += stepx; fraction -= dy; }
                y0 += stepy; fraction += dx;
                gridX = x0 >> EngineSettings.GridWidthPowerOfTwo;
                gridY = y0 >> EngineSettings.GridHeightPowerOfTwo;
                segment.ExitTime += timeslice;
                if (gridX != segment.GridX || gridY != segment.GridY)
                {
                    var last = segment;
                    segment = new MovementSegment(last, state, new Point(x0, y0), last.ExitTime, gridX, gridY)
                        { ExitTime = last.ExitTime };
                    last.Next = segment;
                    AddSegment(segment, gridWidth, gridHeight);
                }
                segment.EndingPoint = new Point(x0, y0);
            }
        }
        segment.ExitTime = 0;
    }

    internal void AddSegment(MovementSegment segment, int gridWidth, int gridHeight)
    {
        var cellRadius = segment.State.CellRadius;
        if (segment.Previous == null)
        {
            Debug.Assert(segment.GridX >= 0 && segment.GridY >= 0 &&
                segment.GridX - cellRadius >= 0 && segment.GridY - cellRadius >= 0 &&
                segment.GridX + cellRadius < gridWidth && segment.GridY + cellRadius < gridHeight);
            Debug.Assert(segment.EntryTime == 0);
            StartSegments.Add(segment);
        }
        else
        {
            Debug.Assert(segment.EntryTime != 0);
            if (segment.GridX < 0 || segment.GridY < 0 ||
                segment.GridX - cellRadius < 0 || segment.GridY - cellRadius < 0 ||
                segment.GridX + cellRadius > gridWidth - 1 || segment.GridY + cellRadius > gridHeight - 1)
            {
                segment.Previous.Next = null;
                return;
            }
        }
        for (var x = segment.GridX - cellRadius; x <= segment.GridX + cellRadius; x++)
        {
            AddWrapperToCell(segment, x, segment.GridY - cellRadius);
            AddWrapperToCell(segment, x, segment.GridY + cellRadius);
        }
        for (var y = segment.GridY - cellRadius + 1; y <= segment.GridY + cellRadius - 1; y++)
        {
            AddWrapperToCell(segment, segment.GridX - cellRadius, y);
            AddWrapperToCell(segment, segment.GridX + cellRadius, y);
        }
    }

    private void AddWrapperToCell(MovementSegment segment, int x, int y)
    {
        var hash = (x << 16) | y;
        if (!_gridSquares.TryGetValue(hash, out var list))
        {
            list = new List<SegmentWrapper>();
            _gridSquares[hash] = list;
        }
        Debug.Assert(segment.EntryTime != 0 || !HasStartingSegments(list));
        var wrapper = new SegmentWrapper(segment, list);
        list.Add(wrapper);
        _sortedList.Add(wrapper);
        segment.CellsLeftToResolve++;
    }

    internal static bool HasStartingSegments(List<SegmentWrapper> list)
    {
        foreach (var w in list)
            if (w.Segment.EntryTime == 0) return true;
        return false;
    }

    public class SegmentWrapper
    {
        public SegmentWrapper(MovementSegment segment, List<SegmentWrapper> parentList)
        { Segment = segment; ParentList = parentList; }
        public MovementSegment Segment { get; set; }
        public List<SegmentWrapper> ParentList { get; set; }
    }

    private class SegmentWrapperComparer : IComparer<SegmentWrapper>
    {
        public int Compare(SegmentWrapper? x, SegmentWrapper? y)
        {
            if (x == null || y == null) return 0;
            var diff = x.Segment.EntryTime - y.Segment.EntryTime;
            return diff == 0 ? x.Segment.State.GetHashCode() - y.Segment.State.GetHashCode() : diff;
        }
    }
}
