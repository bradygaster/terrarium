// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>
/// Base class for all actions a creature can perform.
/// </summary>
public abstract class Action
{
    internal Action(string organismID, int actionID)
    {
        OrganismID = organismID;
        ActionID = actionID;
    }

    public string OrganismID { get; private set; }

    public int ActionID { get; private set; }
}
