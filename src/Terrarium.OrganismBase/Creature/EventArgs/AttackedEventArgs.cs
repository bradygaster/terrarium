// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>
/// Arguments for the Attacked event, containing the state of the attacking creature.
/// </summary>
public class AttackedEventArgs : OrganismEventArgs
{
    public AttackedEventArgs(AnimalState attacker)
    {
        Attacker = attacker;
    }

    public AnimalState Attacker { get; private set; }

    public override string ToString()
    {
        return string.Format("#Attacked {{Attacker = {0}}}", Attacker.ID);
    }
}
