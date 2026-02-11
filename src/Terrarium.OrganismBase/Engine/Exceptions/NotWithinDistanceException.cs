// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>Too far away to perform action.</summary>
public class NotWithinDistanceException : OrganismException
{
    internal NotWithinDistanceException() : base("Too far away to perform action.") { }
}
