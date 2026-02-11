// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>
/// Provides access to a creature's Antenna. Each creature has two
/// Antenna that can be placed in 10 different positions each.
/// </summary>
public class AntennaState
{
    private bool immutable;
    private AntennaPosition leftAntenna;
    private AntennaPosition rightAntenna;

    public AntennaState(AntennaState? state)
    {
        leftAntenna = AntennaPosition.Left;
        rightAntenna = AntennaPosition.Left;

        if (state != null)
        {
            if (VerifyAntenna(state.LeftAntenna))
                leftAntenna = state.LeftAntenna;

            if (VerifyAntenna(state.RightAntenna))
                rightAntenna = state.RightAntenna;
        }
    }

    public AntennaState(AntennaPosition left, AntennaPosition right)
    {
        leftAntenna = VerifyAntenna(left) ? left : AntennaPosition.Left;
        rightAntenna = VerifyAntenna(right) ? right : AntennaPosition.Left;
    }

    public AntennaPosition LeftAntenna
    {
        get => leftAntenna;
        set
        {
            if (!immutable && VerifyAntenna(value))
                leftAntenna = value;
        }
    }

    public AntennaPosition RightAntenna
    {
        get => rightAntenna;
        set
        {
            if (!immutable && VerifyAntenna(value))
                rightAntenna = value;
        }
    }

    public int AntennaValue
    {
        get
        {
            var leftVal = (int)leftAntenna;
            var rightVal = (int)rightAntenna;
            return leftVal * 10 + rightVal;
        }
        set
        {
            if (!immutable && value >= 0 && value < 100)
            {
                leftAntenna = (AntennaPosition)(value / 10);
                rightAntenna = (AntennaPosition)(value % 10);
            }
        }
    }

    public void MakeImmutable()
    {
        immutable = true;
    }

    private static bool VerifyAntenna(AntennaPosition pos)
    {
        var antennaValue = (int)pos;
        return antennaValue >= 0 && antennaValue < 10;
    }
}
