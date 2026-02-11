// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>
/// Species information properties applicable only to animals.
/// </summary>
public interface IAnimalSpecies : ISpecies
{
    AnimalSkinFamily SkinFamily { get; }
    Boolean IsCarnivore { get; }
    int EatingSpeedPerUnitRadius { get; }
    int MaximumAttackDamagePerUnitRadius { get; }
    int MaximumDefendDamagePerUnitRadius { get; }
    int MaximumSpeed { get; }
    int InvisibleOdds { get; }
    int EyesightRadius { get; }
}
