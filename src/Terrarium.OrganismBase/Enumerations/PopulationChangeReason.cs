// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>
/// Describes the reason for the death of a creature.
/// </summary>
public enum PopulationChangeReason
{
    NotDead,
    Initial,
    Born,
    OldAge,
    TeleportedTo,
    Starved,
    Sick,
    TeleportedFrom,
    Killed,
    Error,
    SecurityViolation,
    Timeout,
    OrganismBlacklisted
}
