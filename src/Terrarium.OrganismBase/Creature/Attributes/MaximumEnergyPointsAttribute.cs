// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>Determines how much energy your animal can store.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class MaximumEnergyPointsAttribute : PointBasedCharacteristicAttribute
{
    public MaximumEnergyPointsAttribute(int maximumEnergyPoints)
        : base(maximumEnergyPoints, (int)EngineSettings.MaxEnergyMaximumPerUnitRadius) { }

    public int MaximumEnergyPerUnitRadius => (int)((float)EngineSettings.MaxEnergyBasePerUnitRadius + PercentOfMaximum * (float)EngineSettings.MaxEnergyMaximumPerUnitRadius);
}
