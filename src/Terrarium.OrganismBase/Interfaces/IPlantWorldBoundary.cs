// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>
/// Represents a plant's view of the world.
/// </summary>
public interface IPlantWorldBoundary : IOrganismWorldBoundary
{
    PlantState CurrentPlantState { get; }
}
