// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>Organism is already full.</summary>
public class AlreadyFullException : OrganismException
{
    internal AlreadyFullException() : base("Organism is already full.") { }
}
