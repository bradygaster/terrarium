// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>Controls the distance that a plant can spread its seeds (not currently used).</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class SeedSpreadDistanceAttribute : Attribute
{
    public SeedSpreadDistanceAttribute(int seedSpreadDistance)
    {
        if (seedSpreadDistance > EngineSettings.MaxSeedSpreadDistance)
        {
            throw new ApplicationException(
                "You have placed too many points into SeedSpreadDistance.  Please limit this number to " +
                EngineSettings.MaxSeedSpreadDistance + ".");
        }

        SeedSpreadDistance = seedSpreadDistance;
    }

    public int SeedSpreadDistance { get; private set; }
}
