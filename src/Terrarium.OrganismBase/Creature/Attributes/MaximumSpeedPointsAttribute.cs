// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>Determines the top speed your animal can attain.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class MaximumSpeedPointsAttribute : PointBasedCharacteristicAttribute
{
    public MaximumSpeedPointsAttribute(int speedPoints)
        : base(speedPoints, EngineSettings.SpeedMaximum) { }

    public int MaximumSpeed => (int)(EngineSettings.SpeedBase + PercentOfMaximum * EngineSettings.SpeedMaximum);
}
