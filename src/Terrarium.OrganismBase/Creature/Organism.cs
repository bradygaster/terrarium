// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Drawing;
using System.IO;

namespace OrganismBase;

/// <summary>
/// The Organism class is the base class for the Animal/Plant classes.
/// </summary>
public abstract class Organism
{
    private readonly PendingActions inProgressActions = new PendingActions();
    private readonly Random random;
    private int nextActionID;
    private PendingActions pendingActions = new PendingActions();

    protected Organism()
    {
        random = new Random(GetHashCode());
    }

    public TraceEventHandler? Trace { get; set; }

    internal bool IsInitialized { get; set; }

    internal IOrganismWorldBoundary? OrganismWorldBoundary { get; private set; }

    public MemoryStream? SerializedStream { get; set; }

    public Random OrganismRandom => random;

    public Point Position => OrganismWorldBoundary!.CurrentOrganismState.Position;

    public OrganismState State => OrganismWorldBoundary!.CurrentOrganismState;

    public int TurnsSkipped { get; private set; }

    internal int InternalTurnsSkipped
    {
        get => TurnsSkipped;
        set => TurnsSkipped = value;
    }

    public Boolean CanReproduce =>
        State.ReadyToReproduce && !IsReproducing &&
        State.IsMature && (State.EnergyState >= EnergyState.Normal);

    public ReproduceAction? CurrentReproduceAction =>
        PendingActions.ReproduceAction ?? InProgressActions.ReproduceAction;

    public Boolean IsReproducing => CurrentReproduceAction != null;

    public string ID => OrganismWorldBoundary!.ID;

    internal PendingActions PendingActions => pendingActions;

    internal PendingActions InProgressActions => inProgressActions;

    internal Boolean IsTracing => Trace != null;

    protected virtual void Initialize() { }

    public void SetWorldBoundary(IOrganismWorldBoundary boundary)
    {
        if (OrganismWorldBoundary != null) return;
        OrganismWorldBoundary = boundary;
    }

    public double DistanceTo(OrganismState organismState)
    {
        if (organismState == null)
            throw new ArgumentNullException(nameof(organismState), "The argument 'organismState' cannot be null");

        return Vector.Subtract(Position, organismState.Position).Magnitude;
    }

    public void BeginReproduction(byte[]? dna)
    {
        if (IsReproducing) throw new AlreadyReproducingException();
        if (!State.IsMature) throw new NotMatureException();
        if (State.EnergyState < EnergyState.Normal) throw new NotEnoughEnergyException();
        if (!State.ReadyToReproduce) throw new NotReadyToReproduceException();

        var actionID = GetNextActionID();
        var action = new ReproduceAction(ID, actionID, dna);
        lock (PendingActions)
        {
            PendingActions.SetReproduceAction(action);
            InProgressActions.SetReproduceAction(action);
        }
    }

    internal int GetNextActionID() => nextActionID++;

    public PendingActions GetThenErasePendingActions()
    {
        PendingActions detachedActions;
        lock (PendingActions)
        {
            detachedActions = pendingActions;
            pendingActions = new PendingActions();
        }
        detachedActions.MakeImmutable();
        return detachedActions;
    }

    public abstract void InternalMain(bool clearOnly);

    private void InternalTrace(params object[] tracings)
    {
        var strings = new string[tracings.Length];
        for (var i = 0; i < tracings.Length; i++)
        {
            strings[i] = tracings[i].ToString()!;
            strings[i] = strings[i].Substring(0, (strings[i].Length > 8000) ? 8000 : strings[i].Length);
        }
        Trace!(this, strings);
    }

    public void WriteTrace(object item1)
    {
        if (Trace != null) InternalTrace(item1);
    }

    public void WriteTrace(object item1, object item2)
    {
        if (Trace != null) InternalTrace(item1, item2);
    }

    public void WriteTrace(object item1, object item2, object item3)
    {
        if (Trace != null) InternalTrace(item1, item2, item3);
    }

    public void WriteTrace(object item1, object item2, object item3, object item4)
    {
        if (Trace != null) InternalTrace(item1, item2, item3, item4);
    }

    public void InternalOrganismSerialize(MemoryStream m) { }

    public void InternalOrganismDeserialize(MemoryStream m) { }
}
