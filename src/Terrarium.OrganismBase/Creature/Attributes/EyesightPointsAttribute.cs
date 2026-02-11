// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>Determines how far your animal can see.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class EyesightPointsAttribute : PointBasedCharacteristicAttribute
{
    public EyesightPointsAttribute(int eyesightPoints)
        : base(eyesightPoints, EngineSettings.MaximumEyesightRadius) { }

    public int EyesightRadius => (int)(EngineSettings.BaseEyesightRadius + PercentOfMaximum * EngineSettings.MaximumEyesightRadius);
}
