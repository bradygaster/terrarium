// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>
/// Holds all species related information common to both plants and animals.
/// </summary>
public interface ISpecies
{
    int MatureRadius { get; }
    int ReproductionWait { get; }
    int LifeSpan { get; }
    int GrowthWait { get; }
    int MaximumEnergyPerUnitRadius { get; }
    string Skin { get; }
    bool IsSameSpecies(ISpecies species);
}
