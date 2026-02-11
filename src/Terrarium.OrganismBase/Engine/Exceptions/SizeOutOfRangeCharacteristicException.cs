// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>Size must be within a certain bounds.</summary>
public class SizeOutOfRangeCharacteristicException : GameEngineException
{
    public SizeOutOfRangeCharacteristicException()
        : base("Size must be <= " + EngineSettings.MaxMatureSize + " and >= " + EngineSettings.MinMatureSize) { }
}
