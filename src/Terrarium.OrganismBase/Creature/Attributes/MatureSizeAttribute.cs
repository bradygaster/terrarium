// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>Determines how large your organism will be.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class MatureSizeAttribute : Attribute
{
    private readonly int _matureSize;

    public MatureSizeAttribute(int matureSize)
    {
        if (matureSize > EngineSettings.MaxMatureSize || matureSize < EngineSettings.MinMatureSize)
        {
            throw new SizeOutOfRangeCharacteristicException();
        }

        _matureSize = matureSize;
    }

    public int MatureRadius => _matureSize / 2;
}
