// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Diagnostics;
using OrganismBase;

namespace Terrarium.Game;

/// <summary>
/// Contains information needed to initialize a new organism.
/// </summary>
public class NewOrganism
{
    private readonly OrganismState _state;
    private readonly byte[]? _dna;

    public NewOrganism(OrganismState state, byte[]? dna)
    {
        Debug.Assert(!state.IsImmutable);
        _state = state;
        _dna = dna;
    }

    public OrganismState State => _state;
    public bool AddAtRandomLocation { get; set; } = true;
    public byte[] Dna => _dna != null ? (byte[])_dna.Clone() : Array.Empty<byte>();
}
