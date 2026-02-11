// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>Maximum damage your animal can inflict with one attack.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class AttackDamagePointsAttribute : PointBasedCharacteristicAttribute
{
    public AttackDamagePointsAttribute(int attackPoints)
        : base(attackPoints, EngineSettings.MaximumInflictedDamagePerUnitOfRadius) { }

    public int MaximumAttackDamagePerUnitRadius => (int)((EngineSettings.BaseInflictedDamagePerUnitOfRadius + PercentOfMaximum * EngineSettings.MaximumInflictedDamagePerUnitOfRadius) + 0.001f);
}
