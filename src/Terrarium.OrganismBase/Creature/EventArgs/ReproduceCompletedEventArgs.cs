// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>
/// Arguments for the ReproduceCompleted event.
/// </summary>
public class ReproduceCompletedEventArgs : ActionResponseEventArgs
{
    public ReproduceCompletedEventArgs(int actionID, Action action) : base(actionID, action) { }

    public ReproduceAction ReproduceAction => (ReproduceAction)Action;

    public override string ToString()
    {
        return string.Format("#ReproduceCompleted {{{0}}}", base.ToString());
    }
}
