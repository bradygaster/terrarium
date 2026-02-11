// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>
/// Represents the view available to an organism.
/// </summary>
public interface IOrganismWorldBoundary
{
    OrganismState CurrentOrganismState { get; }
    string ID { get; }
    int WorldWidth { get; }
    int WorldHeight { get; }
}
