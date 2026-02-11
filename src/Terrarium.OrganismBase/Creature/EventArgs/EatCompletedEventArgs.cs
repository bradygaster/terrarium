// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>
/// Arguments for the EatCompleted event.
/// </summary>
public class EatCompletedEventArgs : ActionResponseEventArgs
{
    public EatCompletedEventArgs(int actionID, Action action, Boolean successful) : base(actionID, action)
    {
        Successful = successful;
    }

    public EatAction EatAction => (EatAction)Action;

    public bool Successful { get; private set; }

    public override string ToString()
    {
        return string.Format("#EatCompleted {{Successful={0}, {1}}}", Successful, base.ToString());
    }
}
