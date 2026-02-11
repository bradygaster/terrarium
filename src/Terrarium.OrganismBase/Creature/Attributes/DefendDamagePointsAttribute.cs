// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>Maximum damage your animal can defend against.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class DefendDamagePointsAttribute : PointBasedCharacteristicAttribute
{
    public DefendDamagePointsAttribute(int defensePoints)
        : base(defensePoints, EngineSettings.MaximumDefendedDamagePerUnitOfRadius) { }

    public int MaximumDefendDamagePerUnitRadius => (int)((EngineSettings.BaseDefendedDamagePerUnitOfRadius + PercentOfMaximum * EngineSettings.MaximumDefendedDamagePerUnitOfRadius) + 0.001f);
}
