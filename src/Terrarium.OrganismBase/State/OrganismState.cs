// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;

namespace OrganismBase;

/// <summary>
/// All properties of an organism used by the game. Each OrganismState is immutable
/// and references can be held as long as the organism needs them.
/// </summary>
public abstract class OrganismState : IComparable
{
    protected int currentFoodChunks;
    private MoveToAction? currentMoveToAction;
    private Point currentPosition;
    private ReproduceAction? currentReproduceAction;
    private double energy = 1;
    private OrganismEventResults? events;
    private bool lockedSizeAndPosition;

    internal OrganismState(string id, ISpecies species, int generation, EnergyState initialEnergyState, int initialRadius)
    {
        DeathReason = PopulationChangeReason.NotDead;
        ID = id;
        Species = species;
        Generation = generation;
        SetStoredEnergyInternal(OrganismState.UpperBoundaryForEnergyState(species, initialEnergyState, initialRadius));
        Radius = initialRadius;
        events = new OrganismEventResults();
    }

    public bool IsImmutable { get; private set; }

    public virtual DisplayAction PreviousDisplayAction
    {
        get
        {
            if (!IsAlive) return DisplayAction.Dead;
            if (OrganismEvents != null && OrganismEvents.Teleported != null) return DisplayAction.Teleported;
            if (OrganismEvents != null && OrganismEvents.AttackCompleted != null) return DisplayAction.Attacked;
            if (OrganismEvents != null && OrganismEvents.EatCompleted != null) return DisplayAction.Ate;
            if ((OrganismEvents != null && OrganismEvents.MoveCompleted != null) || IsStopped != true) return DisplayAction.Moved;
            if (OrganismEvents != null && OrganismEvents.DefendCompleted != null) return DisplayAction.Defended;
            if ((OrganismEvents != null && OrganismEvents.ReproduceCompleted != null) || IsIncubating) return DisplayAction.Reproduced;
            return DisplayAction.NoAction;
        }
    }

    public object? RenderInfo { get; set; }

    public OrganismEventResults? OrganismEvents
    {
        get => events;
        set
        {
            if (IsImmutable) throw new GameEngineException("Object is immutable.");
            events = value;
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public ISpecies Species { get; private set; }

    public Boolean IsMature => Radius == Species.MatureRadius;

    public PopulationChangeReason DeathReason { get; private set; }

    public int TickAge { get; private set; }

    public int Generation { get; private set; }

    public ReproduceAction? CurrentReproduceAction
    {
        get => currentReproduceAction;
        set
        {
            if (IsImmutable) throw new GameEngineException("Object is immutable.");
            if (!IsAlive) throw new GameEngineException("Dead organisms can't reproduce.");
            currentReproduceAction = value;
            if (value == null)
            {
                IncubationTicks = 0;
            }
            else
            {
                Debug.Assert(IncubationTicks == 0, "Organism should not be able to start reproduction while already incubating.");
            }
        }
    }

    public Boolean ReadyToReproduce => ReproductionWait == 0;

    public int ReproductionWait { get; private set; }

    public Boolean IsIncubating => currentReproduceAction != null;

    public int IncubationTicks { get; private set; }

    public int FoodChunks
    {
        get => currentFoodChunks;
        set
        {
            if (IsImmutable) throw new GameEngineException("Object is immutable.");
            if (value <= 0) throw new GameEngineException("If foodchunks <= 0 the organism should be removed from the world.");
            currentFoodChunks = value;
        }
    }

    private void SetStoredEnergyInternal(double newEnergy)
    {
        energy = newEnergy;
    }

    public double StoredEnergy
    {
        get => energy;
        set
        {
            if (IsImmutable) throw new GameEngineException("Object is immutable.");
            if (!IsAlive) throw new GameEngineException("Dead organisms can't change stored energy.");
            if (value <= 0) { Kill(PopulationChangeReason.Starved); return; }
            if (value > (double)Radius * Species.MaximumEnergyPerUnitRadius)
                value = Species.MaximumEnergyPerUnitRadius * (double)Radius;
            energy = value;
        }
    }

    public EnergyState EnergyState
    {
        get
        {
            var energyBuckets = (Species.MaximumEnergyPerUnitRadius * Radius) / 5;
            if (energy > energyBuckets * 4) return EnergyState.Full;
            if (energy > energyBuckets * 2) return EnergyState.Normal;
            if (energy > energyBuckets * 1) return EnergyState.Hungry;
            return energy > 0 ? EnergyState.Deterioration : EnergyState.Dead;
        }
    }

    public double PercentEnergy
    {
        get
        {
            Debug.Assert(((energy / (Species.MaximumEnergyPerUnitRadius * Math.Max(1, Radius))) * 100) <= 100);
            return ((energy / (Species.MaximumEnergyPerUnitRadius * Math.Max(1, Radius))));
        }
    }

    public double PercentLifespanRemaining
    {
        get
        {
            Debug.Assert(1 - ((TickAge / (double)(Species.LifeSpan)) * 100) <= 100);
            return (1 - (TickAge / (double)(Species.LifeSpan)));
        }
    }

    public abstract double PercentInjured { get; }

    public Point Position
    {
        get => new Point(currentPosition.X, currentPosition.Y);
        set
        {
            if (IsImmutable) throw new GameEngineException("Object is immutable.");
            if (lockedSizeAndPosition) throw new GameEngineException("Objects position and size are locked.");
            if (!IsAlive) throw new GameEngineException("Dead organisms can't move.");
            currentPosition.X = value.X;
            currentPosition.Y = value.Y;
            SetBitmapDirection();
        }
    }

    public int GridX => Position.X >> EngineSettings.GridWidthPowerOfTwo;
    public int GridY => Position.Y >> EngineSettings.GridHeightPowerOfTwo;

    public int CellRadius
    {
        get
        {
            if (Radius % EngineSettings.GridCellWidth > 0)
                return (Radius >> EngineSettings.GridWidthPowerOfTwo) + 1;
            return Radius >> EngineSettings.GridWidthPowerOfTwo;
        }
    }

    public int Radius { get; private set; }
    public string ID { get; private set; }

    public MoveToAction? CurrentMoveToAction
    {
        get => currentMoveToAction;
        set
        {
            if (IsImmutable) throw new GameEngineException("Object is immutable.");
            if (!IsAlive) throw new GameEngineException("Dead organisms can't move.");
            currentMoveToAction = value;
            SetBitmapDirection();
        }
    }

    public int Speed => currentMoveToAction?.MovementVector.Speed ?? 0;

    public int ActualDirection { get; private set; }

    public Boolean IsStopped => currentMoveToAction == null;

    public Boolean IsAlive => StoredEnergy != 0 && DeathReason == PopulationChangeReason.NotDead && PercentEnergy > 0;

    public int GrowthWait { get; private set; }

    public int CompareTo(Object? other)
    {
        if (other is OrganismState state)
            return currentPosition.Y.CompareTo(state.Position.Y);
        return 0;
    }

    public void LockSizeAndPosition()
    {
        if (IsImmutable) throw new GameEngineException("Object is immutable.");
        lockedSizeAndPosition = true;
    }

    public void MakeImmutable()
    {
        if (events != null) events.MakeImmutable();
        IsImmutable = true;
    }

    public abstract OrganismState CloneMutable();

    protected virtual void CopyStateInto(OrganismState newInstance)
    {
        newInstance.Radius = Radius;
        newInstance.currentMoveToAction = currentMoveToAction;
        newInstance.currentReproduceAction = currentReproduceAction;
        newInstance.currentPosition = new Point(currentPosition.X, currentPosition.Y);
        newInstance.energy = energy;
        newInstance.currentFoodChunks = currentFoodChunks;
        newInstance.IncubationTicks = IncubationTicks;
        newInstance.TickAge = TickAge;
        newInstance.ReproductionWait = ReproductionWait;
        newInstance.GrowthWait = GrowthWait;
        newInstance.DeathReason = DeathReason;
        newInstance.ActualDirection = ActualDirection;
        newInstance.RenderInfo = RenderInfo;
    }

    public virtual void AddTickToAge()
    {
        if (IsImmutable) throw new GameEngineException("Object is immutable.");
        if (!IsAlive) throw new ApplicationException("Dead organisms can't age.");
        TickAge++;
        if (GrowthWait != 0) GrowthWait--;
        if (ReproductionWait != 0) ReproductionWait--;
        if (TickAge > Species.LifeSpan) Kill(PopulationChangeReason.OldAge);
    }

    public void ResetReproductionWait()
    {
        if (IsImmutable) throw new GameEngineException("Object is immutable.");
        ReproductionWait = Species.ReproductionWait;
    }

    public void AddIncubationTick()
    {
        if (IsImmutable) throw new GameEngineException("Object is immutable.");
        IncubationTicks++;
    }

    public void BurnEnergy(double energyValue)
    {
        if (IsImmutable) throw new GameEngineException("Object is immutable.");
        if (!IsAlive) throw new GameEngineException("Dead organisms can't change stored energy.");
        if (StoredEnergy - energyValue <= 0)
            Kill(PopulationChangeReason.Starved);
        else
            StoredEnergy = StoredEnergy - energyValue;
    }

    public static double UpperBoundaryForEnergyState(ISpecies species, EnergyState energyState, int radius)
    {
        var energyBuckets = (species.MaximumEnergyPerUnitRadius * (double)radius) / 5;
        switch (energyState)
        {
            case EnergyState.Dead: return 0;
            case EnergyState.Deterioration: return energyBuckets * 1;
            case EnergyState.Hungry: return energyBuckets * 2;
            case EnergyState.Normal: return energyBuckets * 4;
            case EnergyState.Full: return species.MaximumEnergyPerUnitRadius * (double)radius;
            default: throw new ApplicationException("Unknown EnergyState.");
        }
    }

    public virtual void IncreaseRadiusTo(int newRadius)
    {
        if (IsImmutable) throw new GameEngineException("Object is immutable.");
        if (lockedSizeAndPosition) throw new GameEngineException("Objects position and size are locked.");
        if (!IsAlive) throw new GameEngineException("Dead organisms can't grow.");
        if (newRadius <= Radius) throw new GameEngineException("New radius must be bigger than old one.");
        Radius = newRadius;
    }

    private void SetBitmapDirection()
    {
        if (IsImmutable) throw new GameEngineException("Object is immutable.");
        if (CurrentMoveToAction != null)
        {
            var direction = Vector.Subtract(CurrentMoveToAction.MovementVector.Destination, currentPosition);
            var unitVector = direction.GetUnitVector();
            var angle = Math.Acos(unitVector.X);
            if (unitVector.Y < 0) angle = 6.2831853 - angle;
            ActualDirection = (int)((angle / 6.283185) * 360);
        }
    }

    public void Kill(PopulationChangeReason reason)
    {
        if (IsImmutable) throw new GameEngineException("Object is immutable.");
        currentMoveToAction = null;
        energy = 0;
        DeathReason = reason;
    }

    public abstract OrganismState Grow();

    public void ResetGrowthWait()
    {
        if (IsImmutable) throw new GameEngineException("Object is immutable.");
        GrowthWait = Species.GrowthWait;
    }

    public abstract void HealDamage();

    public Boolean IsAdjacentOrOverlapping(OrganismState state2) => IsWithinRect(0, state2);

    public Boolean IsWithinRect(int state1ExtraRadius, OrganismState? state2)
    {
        if (null == state2) return false;
        var state1Radius = CellRadius + state1ExtraRadius;
        var state2Radius = state2.CellRadius;

        var difference = (GridX - state1Radius) - (state2.GridX - state2Radius);
        if (difference < 0)
        {
            if (-difference > (state1Radius * 2) + 1) return false;
        }
        else
        {
            if (difference > (state2Radius * 2) + 1) return false;
        }

        difference = (GridY - state1Radius) - (state2.GridY - state2Radius);
        if (difference < 0)
        {
            if (-difference > (state1Radius * 2) + 1) return false;
        }
        else
        {
            if (difference > (state2Radius * 2) + 1) return false;
        }

        return true;
    }
}
