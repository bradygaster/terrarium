// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.IO;

namespace OrganismBase;

/// <summary>
/// This is the class that you derive from when you create an animal.
/// </summary>
public abstract class Animal : Organism
{
    private AntennaState antennaState = new AntennaState((AntennaState?)null);

    internal IAnimalWorldBoundary World => (IAnimalWorldBoundary)OrganismWorldBoundary!;

    public AntennaState Antennas
    {
        get => antennaState;
        set => antennaState = value;
    }

    public int WorldWidth => World.WorldWidth;

    public int WorldHeight => World.WorldHeight;

    public new AnimalState State => World.CurrentAnimalState;

    public IAnimalSpecies Species => (IAnimalSpecies)State.Species;

    public MoveToAction? CurrentMoveToAction =>
        PendingActions.MoveToAction ?? InProgressActions.MoveToAction;

    public Boolean IsMoving => CurrentMoveToAction != null;

    public DefendAction? CurrentDefendAction =>
        PendingActions.DefendAction ?? InProgressActions.DefendAction;

    public Boolean IsDefending => CurrentDefendAction != null;

    public AttackAction? CurrentAttackAction =>
        PendingActions.AttackAction ?? InProgressActions.AttackAction;

    public Boolean IsAttacking => CurrentAttackAction != null;

    public EatAction? CurrentEatAction =>
        PendingActions.EatAction ?? InProgressActions.EatAction;

    public Boolean IsEating => CurrentEatAction != null;

    public Boolean CanEat => State.EnergyState <= EnergyState.Normal;

    public event MoveCompletedEventHandler? MoveCompleted;
    public event AttackCompletedEventHandler? AttackCompleted;
    public event EatCompletedEventHandler? EatCompleted;
    public event IdleEventHandler? Idle;
    public event LoadEventHandler? Load;
    public event TeleportedEventHandler? Teleported;
    public event ReproduceCompletedEventHandler? ReproduceCompleted;
    public event BornEventHandler? Born;
    public event DefendCompletedEventHandler? DefendCompleted;
    public event AttackedEventHandler? Attacked;

    public abstract void SerializeAnimal(MemoryStream m);
    public abstract void DeserializeAnimal(MemoryStream m);

    public void InternalAnimalSerialize(MemoryStream m) { }
    public void InternalAnimalDeserialize(MemoryStream m) { }

    public ArrayList Scan() => World.Scan();

    public OrganismState? RefreshState(string organismID)
    {
        if (organismID == null)
            throw new ArgumentNullException(nameof(organismID), "The argument 'organismID' cannot be null");
        return World.RefreshState(organismID);
    }

    public OrganismState? LookFor(OrganismState organismState)
    {
        if (organismState == null)
            throw new ArgumentNullException(nameof(organismState), "The argument 'organismState' cannot be null");
        return World.LookFor(organismState);
    }

    public Boolean IsMySpecies(OrganismState targetState)
    {
        if (targetState == null)
            throw new ArgumentNullException(nameof(targetState), "The argument 'targetState' cannot be null");
        return State.Species.IsSameSpecies(targetState.Species);
    }

    public void StopMoving()
    {
        lock (PendingActions)
        {
            PendingActions.SetMoveToAction(null);
            InProgressActions.SetMoveToAction(null);
        }
    }

    public void BeginMoving(MovementVector vector)
    {
        if (vector == null)
            throw new ArgumentNullException(nameof(vector), "The argument 'vector' cannot be null");

        if (vector.Speed > State.AnimalSpecies.MaximumSpeed)
            throw new TooFastException();

        if (vector.Destination.X > World.WorldWidth - 1 ||
            vector.Destination.X < 0 ||
            vector.Destination.Y > World.WorldHeight - 1 ||
            vector.Destination.Y < 0)
        {
            throw new OutOfBoundsException();
        }

        var actionID = GetNextActionID();
        var action = new MoveToAction(ID, actionID, vector);
        lock (PendingActions)
        {
            PendingActions.SetMoveToAction(action);
            InProgressActions.SetMoveToAction(action);
        }
    }

    public void BeginDefending(AnimalState targetAnimal)
    {
        if (targetAnimal == null)
            throw new ArgumentNullException(nameof(targetAnimal), "The argument 'targetAnimal' cannot be null");

        var actionID = GetNextActionID();
        var action = new DefendAction(ID, actionID, targetAnimal);
        lock (PendingActions)
        {
            PendingActions.SetDefendAction(action);
            InProgressActions.SetDefendAction(action);
        }
    }

    public void BeginAttacking(AnimalState targetAnimal)
    {
        if (targetAnimal == null)
            throw new ArgumentNullException(nameof(targetAnimal), "The argument 'targetAnimal' cannot be null");

        if (!CanAttack(targetAnimal))
            throw new NotHungryException();

        var actionID = GetNextActionID();
        var action = new AttackAction(ID, actionID, targetAnimal);
        lock (PendingActions)
        {
            PendingActions.SetAttackAction(action);
            InProgressActions.SetAttackAction(action);
        }
    }

    public Boolean WithinEatingRange(OrganismState targetOrganism)
    {
        if (targetOrganism == null)
            throw new ArgumentNullException(nameof(targetOrganism), "The argument 'targetOrganism' cannot be null");
        return State.IsAdjacentOrOverlapping(targetOrganism);
    }

    public Boolean WithinAttackingRange(AnimalState targetOrganism)
    {
        if (targetOrganism == null)
            throw new ArgumentNullException(nameof(targetOrganism), "The argument 'targetOrganism' cannot be null");
        return State.IsWithinRect(1, targetOrganism);
    }

    public void BeginEating(OrganismState targetOrganism)
    {
        if (targetOrganism == null)
            throw new ArgumentNullException(nameof(targetOrganism), "The argument 'targetOrganism' cannot be null");

        if (State.EnergyState > EnergyState.Normal)
            throw new AlreadyFullException();

        var currentOrganism = World.LookForNoCamouflage(targetOrganism);
        if (currentOrganism == null)
            throw new NotVisibleException();
        if (!WithinEatingRange(currentOrganism))
            throw new NotWithinDistanceException();

        if (State.AnimalSpecies.IsCarnivore)
        {
            if (currentOrganism is PlantState)
                throw new ImproperFoodException();
            if (currentOrganism.IsAlive)
                throw new NotDeadException();
        }
        else
        {
            if (currentOrganism is AnimalState)
                throw new ImproperFoodException();
        }

        var actionID = GetNextActionID();
        var action = new EatAction(ID, actionID, targetOrganism);
        lock (PendingActions)
        {
            PendingActions.SetEatAction(action);
            InProgressActions.SetEatAction(action);
        }
    }

    public Boolean CanAttack(AnimalState targetAnimal)
    {
        if (State.AnimalSpecies.IsCarnivore) return true;

        if (targetAnimal == null)
            throw new ArgumentNullException(nameof(targetAnimal), "The argument 'targetAnimal' cannot be null");

        var wasAttacked = false;
        foreach (var attackEvent in State.OrganismEvents!.AttackedEvents)
        {
            if (attackEvent.Attacker.ID != targetAnimal.ID) continue;
            wasAttacked = true;
            break;
        }

        return wasAttacked || State.EnergyState <= EnergyState.Hungry;
    }

    public override sealed void InternalMain(bool clearOnly)
    {
        if (!IsInitialized)
        {
            Initialize();
            IsInitialized = true;
        }

        WriteTrace("#Load");
        OnLoad(new LoadEventArgs(), clearOnly);

        var events = State.OrganismEvents;
        if (events != null)
        {
            if (events.MoveCompleted != null) OnMoveCompleted(events.MoveCompleted, clearOnly);
            if (events.AttackCompleted != null) OnAttackCompleted(events.AttackCompleted, clearOnly);
            if (events.EatCompleted != null) OnEatCompleted(events.EatCompleted, clearOnly);
            if (events.Teleported != null) OnTeleported(events.Teleported, clearOnly);
            if (events.ReproduceCompleted != null) OnReproduceCompleted(events.ReproduceCompleted, clearOnly);
            if (events.Born != null) OnBorn(events.Born, clearOnly);
            if (events.DefendCompleted != null) OnDefendCompleted(events.DefendCompleted, clearOnly);
            if (events.AttackedEvents.Count > 0)
            {
                foreach (var attackEvent in events.AttackedEvents)
                {
                    OnAttacked(attackEvent, clearOnly);
                }
            }
        }

        WriteTrace("#Idle");
        OnIdle(new IdleEventArgs(), clearOnly);

        if (clearOnly) InternalTurnsSkipped++;
        else InternalTurnsSkipped = 0;
    }

    private void OnBorn(BornEventArgs e, bool clearOnly) { if (!clearOnly) Born?.Invoke(this, e); }
    private void OnAttacked(AttackedEventArgs e, bool clearOnly) { if (!clearOnly) Attacked?.Invoke(this, e); }
    private void OnIdle(IdleEventArgs e, bool clearOnly) { if (!clearOnly) Idle?.Invoke(this, e); }
    private void OnLoad(LoadEventArgs e, bool clearOnly) { if (!clearOnly) Load?.Invoke(this, e); }

    private void OnReproduceCompleted(ReproduceCompletedEventArgs e, bool clearOnly)
    {
        if (InProgressActions.ReproduceAction != null && e.ActionID == InProgressActions.ReproduceAction.ActionID)
            InProgressActions.SetReproduceAction(null);
        if (!clearOnly) ReproduceCompleted?.Invoke(this, e);
    }

    private void OnDefendCompleted(DefendCompletedEventArgs e, bool clearOnly)
    {
        if (InProgressActions.DefendAction != null && e.ActionID == InProgressActions.DefendAction.ActionID)
            InProgressActions.SetDefendAction(null);
        if (!clearOnly) DefendCompleted?.Invoke(this, e);
    }

    private void OnTeleported(TeleportedEventArgs e, bool clearOnly) { if (!clearOnly) Teleported?.Invoke(this, e); }

    private void OnMoveCompleted(MoveCompletedEventArgs e, bool clearOnly)
    {
        if (InProgressActions.MoveToAction != null && e.ActionID == InProgressActions.MoveToAction.ActionID)
            InProgressActions.SetMoveToAction(null);
        if (!clearOnly) MoveCompleted?.Invoke(this, e);
    }

    private void OnAttackCompleted(AttackCompletedEventArgs e, bool clearOnly)
    {
        if (InProgressActions.AttackAction != null && e.ActionID == InProgressActions.AttackAction.ActionID)
            InProgressActions.SetAttackAction(null);
        if (!clearOnly) AttackCompleted?.Invoke(this, e);
    }

    private void OnEatCompleted(EatCompletedEventArgs e, bool clearOnly)
    {
        if (InProgressActions.EatAction != null && e.ActionID == InProgressActions.EatAction.ActionID)
            InProgressActions.SetEatAction(null);
        if (!clearOnly) EatCompleted?.Invoke(this, e);
    }
}
