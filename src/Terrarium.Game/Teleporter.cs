// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Drawing;
using OrganismBase;

namespace Terrarium.Game;

/// <summary>
/// Manages a set of moving teleport zones.
/// </summary>
public class Teleporter
{
    private readonly TeleportZone[] _teleportZones;
    private Random _random = new(DateTime.Now.Millisecond);

    public Teleporter(int zoneCount)
    {
        if (zoneCount == 0) zoneCount = 1;
        _teleportZones = new TeleportZone[zoneCount];
        for (var i = 0; i < zoneCount; i++)
            _teleportZones[i] = new TeleportZone(new Rectangle(225, 225, 48, 48), null, i);
    }

    public Teleporter Clone()
    {
        var t = new Teleporter(_teleportZones.Length);
        for (var i = 0; i < _teleportZones.Length; i++) t._teleportZones[i] = _teleportZones[i];
        return t;
    }

    public void Move(int worldWidth, int worldHeight)
    {
        for (var i = 0; i < _teleportZones.Length; i++)
        {
            var tz = _teleportZones[i];
            if (tz == null) continue;
            if (tz.Vector == null || tz.Rectangle.Contains(tz.Vector.Destination))
            {
                _teleportZones[i] = tz.SetVector(new MovementVector(
                    new Point(_random.Next(0, worldWidth), _random.Next(0, worldHeight)), 5));
            }
            else
            {
                var r = tz.Rectangle;
                var v = Vector.Subtract(r.Location, tz.Vector.Destination);
                if (v.Magnitude <= tz.Vector.Speed) { _teleportZones[i] = tz.SetVector(null); }
                else
                {
                    var uv = v.GetUnitVector();
                    var sv = uv.Scale(tz.Vector.Speed);
                    r.Location = Vector.Add(r.Location, sv);
                    _teleportZones[i] = tz.SetRectangle(r);
                }
            }
        }
    }

    public bool IsInTeleporter(OrganismState state)
    {
        foreach (var z in _teleportZones) if (z.Contains(state)) return true;
        return false;
    }

    public TeleportZone GetTeleportZone(int id) => _teleportZones[id];
    public TeleportZone[] GetTeleportZones() => (TeleportZone[])_teleportZones.Clone();
}
