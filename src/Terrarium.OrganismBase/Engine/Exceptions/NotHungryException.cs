// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>Organism must be hungry to perform this action.</summary>
public class NotHungryException : OrganismException
{
    internal NotHungryException() : base("Organism must be hungry to perform this action.") { }
}
