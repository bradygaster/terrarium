// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>All points supplied for an organism must add up to 100.</summary>
public class TooManyPointsException : GameEngineException
{
    public TooManyPointsException()
        : base("Total of point-based characteristics must be <= " + EngineSettings.MaxAvailableCharacteristicPoints) { }
}
