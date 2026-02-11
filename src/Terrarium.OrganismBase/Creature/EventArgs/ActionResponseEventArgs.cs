// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>
/// Base class for events that are in response to an action.
/// </summary>
public abstract class ActionResponseEventArgs : OrganismEventArgs
{
    protected ActionResponseEventArgs(int actionID, Action action)
    {
        ActionID = actionID;
        Action = action;
    }

    public int ActionID { get; private set; }

    public Action Action { get; private set; }

    public override string ToString()
    {
        return Action.ToString()!;
    }
}
