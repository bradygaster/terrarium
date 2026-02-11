// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections;

namespace OrganismBase;

/// <summary>
/// Represents an Animal's view of the world.
/// </summary>
public interface IAnimalWorldBoundary : IOrganismWorldBoundary
{
    AnimalState CurrentAnimalState { get; }
    ArrayList Scan();
    OrganismState? LookFor(OrganismState organismState);
    OrganismState? LookForNoCamouflage(OrganismState organismState);
    OrganismState? RefreshState(string organismID);
}
