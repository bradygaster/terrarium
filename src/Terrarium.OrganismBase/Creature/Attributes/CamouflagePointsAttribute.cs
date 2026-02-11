// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>Determines how easily your animal can be seen by other animals.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class CamouflagePointsAttribute : PointBasedCharacteristicAttribute
{
    public CamouflagePointsAttribute(int camouflagePoints)
        : base(camouflagePoints, EngineSettings.InvisibleOddsMaximum) { }

    public int InvisibleOdds => (int)(EngineSettings.InvisibleOddsBase + PercentOfMaximum * EngineSettings.InvisibleOddsMaximum);
}
