// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>
/// Species information properties applicable only to plants.
/// </summary>
public interface IPlantSpecies : ISpecies
{
    PlantSkinFamily SkinFamily { get; }
}
