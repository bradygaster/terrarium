// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Diagnostics;
using OrganismBase;

namespace Terrarium.Game;

/// <summary>
/// Contains the full state of the world at a given tick. Holds state objects
/// for all organisms. Immutable once a game tick is finalized.
/// Replaces legacy Hashtable/ArrayList with Dictionary/List.
/// </summary>
public sealed class WorldState
{
    private readonly OrganismState?[,] _cellOrganisms;
    private readonly int _gridWidth;
    private readonly int _gridHeight;

    private readonly byte[,] _invisible =
        new byte[
            (EngineSettings.MaximumEyesightRadius + EngineSettings.BaseEyesightRadius + EngineSettings.MaxGridRadius) * 2 + 1,
            (EngineSettings.MaximumEyesightRadius + EngineSettings.BaseEyesightRadius + EngineSettings.MaxGridRadius) * 2 + 1];

    private readonly Dictionary<string, OrganismState> _organisms = new();
    private bool _indexBuilt;
    private bool _isImmutable;
    private List<OrganismState>? _orgs;
    private Guid _stateGuid;
    private Teleporter? _teleporter;
    private int _tickNumber;

    public WorldState(int gridWidth, int gridHeight)
    {
        _gridWidth = gridWidth;
        _gridHeight = gridHeight;
        _cellOrganisms = new OrganismState?[gridWidth, gridHeight];
    }

    public bool IndexBuilt => _indexBuilt;
    public ICollection<OrganismState> Organisms => _organisms.Values;

    public IReadOnlyList<OrganismState> ZOrderedOrganisms
    {
        get
        {
            if (_orgs == null)
            {
                _orgs = new List<OrganismState>(_organisms.Values);
                _orgs.Sort();
            }
            return _orgs;
        }
    }

    public Guid StateGuid
    {
        get => _stateGuid;
        set
        {
            if (IsImmutable) throw new InvalidOperationException("WorldState is immutable.");
            _stateGuid = value;
        }
    }

    public int TickNumber
    {
        get => _tickNumber;
        set
        {
            if (IsImmutable) throw new InvalidOperationException("WorldState is immutable.");
            _tickNumber = value;
        }
    }

    public Teleporter? Teleporter
    {
        get => _teleporter;
        set
        {
            if (IsImmutable) throw new InvalidOperationException("WorldState is immutable.");
            _teleporter = value;
        }
    }

    public ICollection<string> OrganismIDs => _organisms.Keys;
    public bool IsImmutable => _isImmutable;

    public WorldState DuplicateMutable()
    {
        var newState = new WorldState(_gridWidth, _gridHeight);
        foreach (var state in Organisms)
        {
            var ns = state.CloneMutable();
            Debug.Assert(ns != null);
            Debug.Assert(ns.ID != null);
            newState._organisms.Add(ns.ID, ns);
        }
        newState._tickNumber = _tickNumber;
        newState._stateGuid = _stateGuid;
        if (_teleporter != null) newState._teleporter = _teleporter.Clone();
        return newState;
    }

    public void ClearIndex()
    {
        if (_isImmutable) throw new InvalidOperationException("WorldState must be mutable.");
        for (var x = 0; x < _gridWidth; x++)
            for (var y = 0; y < _gridHeight; y++)
                _cellOrganisms[x, y] = null;
        _indexBuilt = false;
    }

    public void BuildIndex() => BuildIndexInternal(false);

    private void BuildIndexInternal(bool isDeserializing)
    {
        if (!isDeserializing && _isImmutable)
            throw new InvalidOperationException("WorldState must be mutable to rebuild index.");
        if (_organisms.Count > 0)
        {
            foreach (var state in Organisms)
            {
                Debug.Assert(state.GridX >= 0 && state.GridX < _gridWidth && state.GridY >= 0 && state.GridY < _gridHeight);
                Debug.Assert(_cellOrganisms[state.GridX, state.GridY] == null);
                FillCells(state, state.GridX, state.GridY, state.CellRadius, false);
                if (!isDeserializing) state.LockSizeAndPosition();
            }
        }
        _indexBuilt = true;
    }

    public void FillCells(OrganismState state, int cellX, int cellY, int cellRadius, bool clear)
    {
        Debug.Assert(cellX - cellRadius >= 0 && cellY - cellRadius + 1 >= 0);
        for (var x = cellX - cellRadius; x <= cellX + cellRadius; x++)
            for (var y = cellY - cellRadius; y <= cellY + cellRadius; y++)
            {
                if (clear)
                {
                    Debug.Assert(_cellOrganisms[x, y] == null || _cellOrganisms[x, y]!.ID == state.ID);
                    _cellOrganisms[x, y] = null;
                }
                else
                {
                    Debug.Assert(_cellOrganisms[x, y] == null);
                    _cellOrganisms[x, y] = state;
                }
            }
    }

    public void AddOrganism(OrganismState state)
    {
        if (IsImmutable) throw new InvalidOperationException("WorldState must be mutable to add organisms.");
        Debug.Assert(state.GridX >= 0 && state.GridX < _gridWidth && state.GridY >= 0 && state.GridY < _gridHeight);
        Debug.Assert(_cellOrganisms[state.GridX, state.GridY] == null);
        if (_organisms.ContainsKey(state.ID))
            throw new InvalidOperationException("Organism already exists in this world state.");
        FillCells(state, state.GridX, state.GridY, state.CellRadius, false);
        state.LockSizeAndPosition();
        _organisms.Add(state.ID, state);
    }

    public void RefreshOrganism(OrganismState state)
    {
        if (IsImmutable) throw new InvalidOperationException("WorldState must be mutable to change.");
        var id = state.ID;
        if (IndexBuilt)
        {
            var oldState = GetOrganismState(id);
            if (oldState != null) FillCells(oldState, oldState.GridX, oldState.GridY, oldState.CellRadius, true);
        }
        _organisms.Remove(id);
        AddOrganism(state);
    }

    public void RemoveOrganism(string organismID)
    {
        if (IsImmutable) throw new InvalidOperationException("WorldState must be mutable to change.");
        if (IndexBuilt)
        {
            var state = GetOrganismState(organismID);
            if (state != null) FillCells(state, state.GridX, state.GridY, state.CellRadius, true);
        }
        _organisms.Remove(organismID);
    }

    public OrganismState? GetOrganismState(string organismID)
    {
        _organisms.TryGetValue(organismID, out var state);
        return state;
    }

    public void MakeImmutable()
    {
        foreach (var state in Organisms) state.MakeImmutable();
        _isImmutable = true;
    }

    public List<OrganismState> FindOrganisms(int x1, int x2, int y1, int y2)
    {
        int minX, maxX, minY, maxY;
        if (x1 < x2) { minX = x1; maxX = x2; } else { minX = x2; maxX = x1; }
        if (y1 < y2) { minY = y1; maxY = y2; } else { minY = y2; maxY = y1; }
        if (minX < 0) minX = 0; if (maxX < 0) maxX = 0;
        if (minY < 0) minY = 0; if (maxY < 0) maxY = 0;
        minX >>= EngineSettings.GridWidthPowerOfTwo;
        if (minX > _gridWidth - 1) minX = _gridWidth - 1;
        maxX >>= EngineSettings.GridWidthPowerOfTwo;
        if (maxX > _gridWidth - 1) maxX = _gridWidth - 1;
        minY >>= EngineSettings.GridHeightPowerOfTwo;
        if (minY > _gridHeight - 1) minY = _gridHeight - 1;
        maxY >>= EngineSettings.GridHeightPowerOfTwo;
        if (maxY > _gridHeight - 1) maxY = _gridHeight - 1;
        return FindOrganismsInCells(minX, maxX, minY, maxY);
    }

    public int GetAvailableLight(PlantState plant)
    {
        var maxX = plant.GridX + plant.CellRadius + 25;
        if (maxX > _gridWidth - 1) maxX = _gridWidth - 1;
        var east = FindOrganismsInCells(plant.GridX + plant.CellRadius, maxX,
            plant.GridY - plant.CellRadius, plant.GridY + plant.CellRadius);
        var minX = plant.GridX - plant.CellRadius - 25;
        if (minX < 0) minX = 0;
        var west = FindOrganismsInCells(minX, plant.GridX - plant.CellRadius,
            plant.GridY - plant.CellRadius, plant.GridY + plant.CellRadius);

        double maxAngleEast = 0;
        foreach (var t in east)
        {
            if (t is not PlantState ps) continue;
            var a = Math.Atan2(ps.Height, t.Position.X - plant.Position.X);
            if (a > maxAngleEast) maxAngleEast = a;
        }
        double maxAngleWest = 0;
        foreach (var t in west)
        {
            if (t is not PlantState ps) continue;
            var a = Math.Atan2(ps.Height, plant.Position.X - t.Position.X);
            if (a > maxAngleWest) maxAngleWest = a;
        }
        return (int)(((Math.PI - maxAngleEast + maxAngleWest) / Math.PI) * 100);
    }

    public bool OnlyOverlapsSelf(OrganismState state)
    {
        Debug.Assert(IndexBuilt);
        var minGX = state.GridX - state.CellRadius; var maxGX = state.GridX + state.CellRadius;
        var minGY = state.GridY - state.CellRadius; var maxGY = state.GridY + state.CellRadius;
        if (minGX < 0 || maxGX > _gridWidth - 1 || minGY < 0 || maxGY > _gridHeight - 1) return false;
        for (var x = minGX; x <= maxGX; x++)
            for (var y = minGY; y <= maxGY; y++)
            {
                if (_cellOrganisms[x, y] == null) continue;
                if (_cellOrganisms[x, y]!.ID != state.ID) return false;
            }
        return true;
    }

    public List<OrganismState> FindOrganismsInCells(int minGridX, int maxGridX, int minGridY, int maxGridY)
    {
        Debug.Assert(IndexBuilt);
        Debug.Assert(minGridX <= maxGridX && minGridY <= maxGridY);
        Debug.Assert(minGridX >= 0 && maxGridX < _gridWidth && minGridY >= 0 && maxGridY < _gridHeight);
        OrganismState? lastFound = null;
        var foundHash = new HashSet<OrganismState>();
        var foundOrganisms = new List<OrganismState>();
        for (var x = minGridX; x <= maxGridX; x++)
            for (var y = minGridY; y <= maxGridY; y++)
            {
                var current = _cellOrganisms[x, y];
                if (current == null) continue;
                if (lastFound != null && lastFound == current) continue;
                if (foundHash.Add(current)) foundOrganisms.Add(current);
                lastFound = current;
            }
        return foundOrganisms;
    }

    public List<OrganismState> FindOrganismsInView(OrganismState state, int radius)
    {
        Debug.Assert(IndexBuilt);
        Debug.Assert((state.CellRadius + radius) * 2 + 1 <= _invisible.GetLength(0));
        var foundOrganisms = new List<OrganismState>();
        var foundHash = new HashSet<OrganismState>();
        var oX = state.GridX; var oY = state.GridY;
        var mX = -oX + state.CellRadius + radius;
        var mY = -oY + state.CellRadius + radius;
        int xI = 0, yI = 0;

        var cr = state.CellRadius + 1;
        var cx = oX - cr; var cy = oY - cr;
        for (var s = 0; s < 4; s++)
        {
            switch (s) { case 0: xI=1; yI=0; break; case 1: xI=0; yI=1; break; case 2: xI=-1; yI=0; break; case 3: xI=0; yI=-1; break; }
            for (var c = 0; c < cr << 1; c++)
            {
                if (cx >= 0 && cy >= 0 && cx < _gridWidth && cy < _gridHeight)
                {
                    var co = _cellOrganisms[cx, cy];
                    if (co != null && foundHash.Add(co)) foundOrganisms.Add(co);
                    _invisible[cx + mX, cy + mY] = 0;
                }
                cx += xI; cy += yI;
            }
        }

        for (cr = state.CellRadius + 2; cr <= state.CellRadius + radius; cr++)
        {
            cx = oX - cr; cy = oY - cr;
            for (var s = 0; s < 4; s++)
            {
                switch (s) { case 0: xI=1; yI=0; break; case 1: xI=0; yI=1; break; case 2: xI=-1; yI=0; break; case 3: xI=0; yI=-1; break; }
                for (var c = 0; c < cr << 1; c++)
                {
                    if (cx >= 0 && cy >= 0 && cx < _gridWidth && cy < _gridHeight)
                    {
                        var i = cx - oX; var j = cy - oY;
                        int sI = i < 0 ? 1 : i > 0 ? -1 : 0;
                        int sJ = j < 0 ? 1 : j > 0 ? -1 : 0;
                        int aI = i < 0 ? -i : i; int aJ = j < 0 ? -j : j;
                        int p1X, p1Y;
                        if (aJ > aI) { p1X = cx; p1Y = sJ + cy; } else { p1X = sI + cx; p1Y = sJ + cy; }
                        int p2X, p2Y;
                        if (aJ != aI) { p2X = sI + cx; p2Y = sJ + cy; } else { p2X = p1X; p2Y = p1Y; }
                        if (_invisible[p1X + mX, p1Y + mY] == 1 || _invisible[p2X + mX, p2Y + mY] == 1)
                            _invisible[cx + mX, cy + mY] = 1;
                        else
                        {
                            var co = _cellOrganisms[cx, cy];
                            if (co != null)
                            {
                                _invisible[cx + mX, cy + mY] = 1;
                                if (foundHash.Add(co)) foundOrganisms.Add(co);
                            }
                            else _invisible[cx + mX, cy + mY] = 0;
                        }
                    }
                    cx += xI; cy += yI;
                }
            }
        }
        return foundOrganisms;
    }

    public bool IsGridCellOccupied(int cellX, int cellY)
    {
        Debug.Assert(IndexBuilt);
        Debug.Assert(cellX < _gridWidth && cellY < _gridHeight && cellX >= 0 && cellY >= 0);
        return _cellOrganisms[cellX, cellY] != null;
    }
}
