// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>
/// Represents a command to defend against an attack.
/// </summary>
public class DefendAction : Action
{
    internal DefendAction(string organismID, int actionID, AnimalState targetAnimal) : base(organismID, actionID)
    {
        TargetAnimal = targetAnimal;
    }

    public AnimalState TargetAnimal { get; private set; }

    public override string ToString()
    {
        return string.Format("TargetAnimal={0}", TargetAnimal.ID);
    }
}
