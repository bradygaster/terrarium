// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>Can only apply 100 points to any given characteristic.</summary>
public class TooManyPointsOnOneCharacteristicException : GameEngineException
{
    public TooManyPointsOnOneCharacteristicException()
        : base("Point-based characteristics must be <= " + EngineSettings.MaxAvailableCharacteristicPoints) { }
}
