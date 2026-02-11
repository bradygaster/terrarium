// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>
/// Used to determine what the most prominent completed action for the previous tick was.
/// </summary>
public enum DisplayAction
{
    Attacked = -1,
    Defended = 7,
    Dead = 4000,
    Died = 15,
    Ate = 23,
    Moved = 31,
    NoAction = 32,
    Teleported = 33,
    Reproduced = 34
}
