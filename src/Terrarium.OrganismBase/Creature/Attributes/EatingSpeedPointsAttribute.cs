// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>Determines how quickly your animal can eat.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class EatingSpeedPointsAttribute : PointBasedCharacteristicAttribute
{
    public EatingSpeedPointsAttribute(int eatingSpeedPoints)
        : base(eatingSpeedPoints, EngineSettings.MaximumEatingSpeedPerUnitOfRadius) { }

    public int EatingSpeedPerUnitRadius => (int)(EngineSettings.BaseEatingSpeedPerUnitOfRadius + PercentOfMaximum * EngineSettings.MaximumEatingSpeedPerUnitOfRadius);
}
