// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>
/// Base class for point-based creature characteristic attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public abstract class PointBasedCharacteristicAttribute : Attribute
{
    private readonly int _maximumValue;
    private readonly int _originalPoints;

    protected PointBasedCharacteristicAttribute(int points, int maximumValue)
    {
        _originalPoints = points;
        _maximumValue = maximumValue;

        if (points > EngineSettings.MaxAvailableCharacteristicPoints || points < 0)
        {
            throw new TooManyPointsOnOneCharacteristicException();
        }

        Points = points;
    }

    public int Points { get; private set; }

    internal float PercentOfMaximum => Points / (float)EngineSettings.MaxAvailableCharacteristicPoints;

    public string GetWarnings()
    {
        if ((_maximumValue / (double)EngineSettings.MaxAvailableCharacteristicPoints) < 1)
        {
            var pointsToIncrement = EngineSettings.MaxAvailableCharacteristicPoints / _maximumValue;
            if (_originalPoints % pointsToIncrement != 0)
            {
                return "Points applied to '" + GetType().Name + "' should be in increments of " + pointsToIncrement +
                       ".  Anything else is wasted.";
            }
        }

        return string.Empty;
    }
}
