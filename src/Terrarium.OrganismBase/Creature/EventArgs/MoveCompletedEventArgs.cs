// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>
/// Arguments for the MoveCompleted event.
/// </summary>
public class MoveCompletedEventArgs : ActionResponseEventArgs
{
    public MoveCompletedEventArgs(int actionID, Action action, ReasonForStop reason,
                                  OrganismState? blockingOrganism) : base(actionID, action)
    {
        Reason = reason;
        BlockingOrganism = blockingOrganism;
    }

    public MoveToAction MoveToAction => (MoveToAction)Action;

    public ReasonForStop Reason { get; private set; }

    public OrganismState? BlockingOrganism { get; private set; }

    public override string ToString()
    {
        return string.Format("#MoveCompleted {{Reason={0}, {1}}}", Reason, base.ToString());
    }
}
