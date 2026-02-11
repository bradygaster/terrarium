// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>
/// Arguments for the Born event, containing DNA from the parent.
/// </summary>
public class BornEventArgs : OrganismEventArgs
{
    public BornEventArgs(byte[]? dna)
    {
        Dna = dna;
    }

    public byte[]? Dna { get; private set; }

    public override string ToString() => "#Born";
}
