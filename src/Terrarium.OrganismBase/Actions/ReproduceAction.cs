// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>
/// Represents a command to reproduce.
/// </summary>
public class ReproduceAction : Action
{
    private readonly byte[]? _dna;

    internal ReproduceAction(string organismID, int actionID, byte[]? dna) : base(organismID, actionID)
    {
        _dna = dna;
    }

    public byte[]? Dna
    {
        get
        {
            if (_dna != null)
                return (byte[])_dna.Clone();
            return null;
        }
    }
}
