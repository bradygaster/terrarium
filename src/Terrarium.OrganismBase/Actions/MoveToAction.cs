// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>
/// Represents a movement command.
/// </summary>
public class MoveToAction : Action
{
    internal MoveToAction(string organismID, int actionID, MovementVector moveTo) : base(organismID, actionID)
    {
        MovementVector = moveTo;
    }

    public MovementVector MovementVector { get; private set; }

    public override string ToString()
    {
        return MovementVector.ToString();
    }
}
