// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>
/// Aggregates the set of pending actions for an organism in one chunk.
/// </summary>
public class PendingActions
{
    public bool IsImmutable { get; private set; }
    public DefendAction? DefendAction { get; private set; }
    public MoveToAction? MoveToAction { get; private set; }
    public AttackAction? AttackAction { get; private set; }
    public EatAction? EatAction { get; private set; }
    public ReproduceAction? ReproduceAction { get; private set; }

    public void MakeImmutable()
    {
        IsImmutable = true;
    }

    public void SetDefendAction(DefendAction? defendAction)
    {
        if (IsImmutable) throw new ApplicationException("PendingActions must be mutable to modify actions.");
        DefendAction = defendAction;
    }

    public void SetMoveToAction(MoveToAction? moveToAction)
    {
        if (IsImmutable) throw new ApplicationException("PendingActions must be mutable to modify actions.");
        MoveToAction = moveToAction;
    }

    public void SetAttackAction(AttackAction? attackAction)
    {
        if (IsImmutable) throw new ApplicationException("PendingActions must be mutable to modify actions.");
        AttackAction = attackAction;
    }

    public void SetEatAction(EatAction? eatAction)
    {
        if (IsImmutable) throw new ApplicationException("PendingActions must be mutable to modify actions.");
        EatAction = eatAction;
    }

    public void SetReproduceAction(ReproduceAction? reproduceAction)
    {
        if (IsImmutable) throw new ApplicationException("PendingActions must be mutable to modify actions.");
        ReproduceAction = reproduceAction;
    }
}
