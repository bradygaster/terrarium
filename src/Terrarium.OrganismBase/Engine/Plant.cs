// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO;

namespace OrganismBase;

/// <summary>
/// This is the base class used by any creatures that want to become a Plant.
/// </summary>
public abstract class Plant : Organism
{
    internal IPlantWorldBoundary World => (IPlantWorldBoundary)OrganismWorldBoundary!;

    public new PlantState State => World.CurrentPlantState;

    public abstract void SerializePlant(MemoryStream m);
    public abstract void DeserializePlant(MemoryStream m);

    public void InternalPlantSerialize(MemoryStream m) { }
    public void InternalPlantDeserialize(MemoryStream m) { }

    public override sealed void InternalMain(bool clearOnly)
    {
        var events = State.OrganismEvents;

        if (!IsInitialized)
        {
            Initialize();
            IsInitialized = true;
        }

        if (events != null)
        {
            if (events.ReproduceCompleted != null)
            {
                if (InProgressActions.ReproduceAction != null &&
                    events.ReproduceCompleted.ActionID == InProgressActions.ReproduceAction.ActionID)
                {
                    InProgressActions.SetReproduceAction(null);
                }
            }
        }

        if (CanReproduce)
        {
            BeginReproduction(null);
        }

        if (clearOnly) InternalTurnsSkipped++;
        else InternalTurnsSkipped = 0;
    }
}
