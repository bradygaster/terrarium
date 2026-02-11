// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>
/// Represents all events that will get sent to an animal for a given tick.
/// </summary>
public class OrganismEventResults
{
    private readonly AttackedEventArgsCollection attackedCollection = new AttackedEventArgsCollection();
    private AttackCompletedEventArgs? attackCompleted;
    private BornEventArgs? born;
    private DefendCompletedEventArgs? defendCompleted;
    private EatCompletedEventArgs? eatCompleted;
    private MoveCompletedEventArgs? moveCompleted;
    private ReproduceCompletedEventArgs? reproduceCompleted;
    private TeleportedEventArgs? teleported;

    public Boolean IsImmutable { get; private set; }

    public BornEventArgs? Born
    {
        get => born;
        set
        {
            if (IsImmutable) throw new ApplicationException("Object is immutable.");
            born = value;
        }
    }

    public ReproduceCompletedEventArgs? ReproduceCompleted
    {
        get => reproduceCompleted;
        set
        {
            if (IsImmutable) throw new ApplicationException("Object is immutable.");
            reproduceCompleted = value;
        }
    }

    public TeleportedEventArgs? Teleported
    {
        get => teleported;
        set
        {
            if (IsImmutable) throw new ApplicationException("Object is immutable.");
            teleported = value;
        }
    }

    public AttackedEventArgsCollection AttackedEvents => attackedCollection;

    public MoveCompletedEventArgs? MoveCompleted
    {
        get => moveCompleted;
        set
        {
            if (IsImmutable) throw new ApplicationException("Object is immutable.");
            moveCompleted = value;
        }
    }

    public AttackCompletedEventArgs? AttackCompleted
    {
        get => attackCompleted;
        set
        {
            if (IsImmutable) throw new ApplicationException("Object is immutable.");
            attackCompleted = value;
        }
    }

    public EatCompletedEventArgs? EatCompleted
    {
        get => eatCompleted;
        set
        {
            if (IsImmutable) throw new ApplicationException("Object is immutable.");
            eatCompleted = value;
        }
    }

    public DefendCompletedEventArgs? DefendCompleted
    {
        get => defendCompleted;
        set
        {
            if (IsImmutable) throw new ApplicationException("Object is immutable.");
            defendCompleted = value;
        }
    }

    public void MakeImmutable()
    {
        IsImmutable = true;
        attackedCollection.MakeImmutable();
    }
}
