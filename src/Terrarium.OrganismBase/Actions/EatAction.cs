// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>
/// Represents a command to eat another creature or plant.
/// </summary>
public class EatAction : Action
{
    internal EatAction(string organismID, int actionID, OrganismState targetOrganism) : base(organismID, actionID)
    {
        TargetOrganism = targetOrganism;
    }

    public OrganismState TargetOrganism { get; private set; }

    public override string ToString()
    {
        return string.Format("TargetOrganism={0}", TargetOrganism.ID);
    }
}
