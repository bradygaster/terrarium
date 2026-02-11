// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.ComponentModel;
using System.Diagnostics;

namespace OrganismBase;

/// <summary>
/// Represents a creature's state during a certain tick in time.
/// </summary>
public sealed class AnimalState : OrganismState
{
    private int damage;
    private AntennaState state = new AntennaState((AntennaState?)null);

    public AnimalState(string id, ISpecies species, int generation, EnergyState initialEnergyState, int initialRadius)
        : base(id, species, generation, initialEnergyState, initialRadius)
    {
    }

    [TypeConverter((typeof(ExpandableObjectConverter)))]
    public AntennaState Antennas
    {
        get
        {
            if (IsImmutable) state.MakeImmutable();
            return state;
        }
        set
        {
            if (!IsImmutable)
            {
                if (value != null) state = value;
            }
            else
            {
                throw new GameEngineException(
                    "Antennas can not be set on the State object.  Use the Antennas property on your Creature class instead.");
            }
        }
    }

    public IAnimalSpecies AnimalSpecies => (IAnimalSpecies)Species;

    public int RotTicks { get; private set; }

    public override double PercentInjured
    {
        get
        {
            Debug.Assert(((damage / (EngineSettings.DamageToKillPerUnitOfRadius * (double)Radius)) * 100) <= 100);
            return ((damage / (EngineSettings.DamageToKillPerUnitOfRadius * (double)Radius)));
        }
    }

    public override DisplayAction PreviousDisplayAction
    {
        get
        {
            if (!IsAlive && RotTicks == 0) return DisplayAction.Died;
            return base.PreviousDisplayAction;
        }
    }

    public int Damage => damage;

    public override OrganismState CloneMutable()
    {
        var newInstance = new AnimalState(ID, AnimalSpecies, Generation, EnergyState, Radius);
        CopyStateInto(newInstance);
        return newInstance;
    }

    protected override void CopyStateInto(OrganismState newInstance)
    {
        base.CopyStateInto(newInstance);
        ((AnimalState)newInstance).damage = damage;
        ((AnimalState)newInstance).RotTicks = RotTicks;
        ((AnimalState)newInstance).state = new AntennaState(state);
    }

    public void AddRotTick()
    {
        if (IsImmutable) throw new GameEngineException("Object is immutable.");
        Debug.Assert(!IsAlive);
        RotTicks++;
    }

    public void CauseDamage(int incrementalDamage)
    {
        if (IsImmutable) throw new GameEngineException("Object is immutable.");
        if (incrementalDamage < 0) throw new GameEngineException("Damage must be positive.");
        if (Damage + incrementalDamage >= EngineSettings.DamageToKillPerUnitOfRadius * Radius)
        {
            Kill(PopulationChangeReason.Killed);
            damage = EngineSettings.DamageToKillPerUnitOfRadius * Radius;
            return;
        }
        damage = damage + incrementalDamage;
    }

    public override void HealDamage()
    {
        if (IsImmutable) throw new GameEngineException("Object is immutable.");
        double maxHealing = EngineSettings.AnimalMaxHealingPerTickPerRadius * Radius;
        Debug.Assert(maxHealing > 0);

        var usableEnergy = StoredEnergy - UpperBoundaryForEnergyState(Species, EnergyState.Hungry, Radius);
        if (usableEnergy > 0)
        {
            var availableHealing = usableEnergy / (EngineSettings.AnimalRequiredEnergyPerUnitOfHealing);
            if (availableHealing < maxHealing) maxHealing = availableHealing;

            double damageDelta;
            if (damage - maxHealing < 0)
            {
                damageDelta = damage;
                damage = 0;
            }
            else
            {
                damageDelta = maxHealing;
                damage = (int)(damage - maxHealing);
            }

            BurnEnergy(damageDelta * EngineSettings.AnimalRequiredEnergyPerUnitOfHealing);
        }
    }

    public override void IncreaseRadiusTo(int newRadius)
    {
        if (IsImmutable) throw new GameEngineException("Object is immutable.");
        var additionalRadius = newRadius - Radius;
        base.IncreaseRadiusTo(newRadius);
        currentFoodChunks += additionalRadius * EngineSettings.FoodChunksPerUnitOfRadius;
    }

    public override OrganismState Grow()
    {
        if (IsImmutable) throw new GameEngineException("Object is immutable.");
        if (Radius < Species.MatureRadius && GrowthWait == 0)
        {
            if (EnergyState >= EnergyState.Normal)
            {
                var newState = (AnimalState)CloneMutable();
                newState.OrganismEvents = OrganismEvents;
                newState.IncreaseRadiusTo(Radius + 1);
                newState.BurnEnergy(EngineSettings.AnimalRequiredEnergyPerUnitOfRadiusGrowth);
                newState.ResetGrowthWait();
                return newState;
            }
        }
        return this;
    }

    public double EnergyRequiredToMove(double distance, int speed)
    {
        return distance * Radius * speed * EngineSettings.RequiredEnergyPerUnitOfRadiusSpeedDistance;
    }
}
