// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>This position is outside the boundaries of the world.</summary>
public class OutOfBoundsException : OrganismException
{
    internal OutOfBoundsException() : base("This position is outside the boundaries of the world.") { }
}
