// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Diagnostics;

namespace OrganismBase;

/// <summary>
/// Represents a plant's state during a certain tick in time.
/// </summary>
public sealed class PlantState : OrganismState
{
    private const double heightToRadiusRatio = 1;
    private const int optimalLightPercentage = 100;

    public PlantState(string id, ISpecies species, int generation, EnergyState initialEnergyState, int initialRadius)
        : base(id, species, generation, initialEnergyState, initialRadius)
    {
    }

    public int Height { get; private set; }

    public int CurrentMaxFoodChunks => Radius * EngineSettings.PlantFoodChunksPerUnitOfRadius;

    public override double PercentInjured
    {
        get
        {
            Debug.Assert(1 - ((FoodChunks / (double)CurrentMaxFoodChunks) * 100) <= 100);
            return 1 - (FoodChunks / (double)CurrentMaxFoodChunks);
        }
    }

    public override OrganismState CloneMutable()
    {
        var newInstance = new PlantState(ID, Species, Generation, EnergyState, Radius);
        CopyStateInto(newInstance);
        return newInstance;
    }

    protected override void CopyStateInto(OrganismState newInstance)
    {
        base.CopyStateInto(newInstance);
        ((PlantState)newInstance).Height = Height;
    }

    public void GiveEnergy(int availableLightPercentage)
    {
        if (IsImmutable) throw new GameEngineException("Object is immutable.");
        var percentageFromOptimal = availableLightPercentage - optimalLightPercentage;
        if (percentageFromOptimal < 0) percentageFromOptimal = -percentageFromOptimal;
        Debug.Assert(percentageFromOptimal <= 100);

        var energyGained = (int)((1 - (percentageFromOptimal / (double)100)) * EngineSettings.MaxEnergyFromLightPerTick);
        StoredEnergy = StoredEnergy + energyGained;
    }

    public override void IncreaseRadiusTo(int newRadius)
    {
        if (IsImmutable) throw new GameEngineException("Object is immutable.");
        var newHeight = (int)(newRadius * heightToRadiusRatio);
        var additionalRadius = newRadius - Radius;
        base.IncreaseRadiusTo(newRadius);
        Height = newHeight;
        currentFoodChunks += additionalRadius * EngineSettings.PlantFoodChunksPerUnitOfRadius;
    }

    public override OrganismState Grow()
    {
        if (IsImmutable) throw new GameEngineException("Object is immutable.");
        if (Radius < Species.MatureRadius)
        {
            if (EnergyState >= EnergyState.Normal && GrowthWait == 0)
            {
                var newState = (PlantState)CloneMutable();
                newState.OrganismEvents = OrganismEvents;
                newState.IncreaseRadiusTo(Radius + 1);
                newState.BurnEnergy(EngineSettings.PlantRequiredEnergyPerUnitOfRadiusGrowth);
                newState.ResetGrowthWait();
                return newState;
            }
        }
        return this;
    }

    public override void HealDamage()
    {
        if (IsImmutable) throw new GameEngineException("Object is immutable.");
        var maxHealingChunks = EngineSettings.PlantMaxHealingPerTickPerRadius * Radius;

        var usableEnergy = StoredEnergy - UpperBoundaryForEnergyState(Species, EnergyState.Deterioration, Radius);
        if (usableEnergy <= 0) return;

        var availableHealingChunks = (int)(usableEnergy / (EngineSettings.PlantRequiredEnergyPerUnitOfHealing));
        if (availableHealingChunks < maxHealingChunks)
            maxHealingChunks = availableHealingChunks;

        int foodChunkDelta;
        if (CurrentMaxFoodChunks - FoodChunks < maxHealingChunks)
        {
            foodChunkDelta = CurrentMaxFoodChunks - FoodChunks;
            currentFoodChunks = CurrentMaxFoodChunks;
        }
        else
        {
            foodChunkDelta = maxHealingChunks;
            currentFoodChunks += foodChunkDelta;
        }

        BurnEnergy(foodChunkDelta * (double)EngineSettings.PlantRequiredEnergyPerUnitOfHealing);
    }
}
