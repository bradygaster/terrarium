// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>
/// Arguments for the AttackCompleted event.
/// </summary>
public class AttackCompletedEventArgs : ActionResponseEventArgs
{
    public AttackCompletedEventArgs(int actionID, Action action, Boolean killed, Boolean escaped,
                                    int inflictedDamage) : base(actionID, action)
    {
        InflictedDamage = inflictedDamage;
        Escaped = escaped;
        Killed = killed;
    }

    public AttackAction AttackAction => (AttackAction)Action;

    public int InflictedDamage { get; private set; }

    public bool Killed { get; private set; }

    public bool Escaped { get; private set; }

    public override string ToString()
    {
        return string.Format("#AttackCompleted {{InflictedDamage={0}, Killed={1}, Escaped={2}, {3}}}",
                             InflictedDamage, Killed, Escaped, base.ToString());
    }
}
