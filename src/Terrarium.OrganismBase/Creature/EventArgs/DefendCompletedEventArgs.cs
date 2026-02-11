// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>
/// Arguments for the DefendCompleted event.
/// </summary>
public class DefendCompletedEventArgs : ActionResponseEventArgs
{
    public DefendCompletedEventArgs(int actionID, Action action) : base(actionID, action) { }

    public DefendAction DefendAction => (DefendAction)Action;

    public override string ToString()
    {
        return string.Format("#DefendCompleted {{{0}}}", base.ToString());
    }
}
