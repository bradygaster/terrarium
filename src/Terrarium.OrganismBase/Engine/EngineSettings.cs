// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Diagnostics;

namespace OrganismBase;

/// <summary>
/// Contains all constants that affect the game world, creature attributes,
/// and computed values for the Terrarium. These are the laws of physics
/// and biology for the Terrarium.
/// </summary>
public class EngineSettings
{
    public const double AnimalIncubationEnergyMultiplier = 1.5;
    public const double AnimalIncubationEnergyPerUnitOfRadius =
        (AnimalMatureSizeProvidedEnergyPerUnitRadius / TicksToIncubate) *
        AnimalIncubationEnergyMultiplier;
    public const int AnimalLifeSpanPerUnitMaximumRadius = 50;
    public const double AnimalMatureSizeProvidedEnergyPerUnitRadius =
        FoodChunksPerUnitOfRadius * EnergyPerAnimalFoodChunk;
    public const int AnimalMaxHealingPerTickPerRadius = 2;
    public const int AnimalReproductionWaitPerUnitRadius = 8;
    public const double AnimalRequiredEnergyPerUnitOfHealing = .1;
    public const double AnimalRequiredEnergyPerUnitOfRadiusGrowth = MaxEnergyBasePerUnitRadius * (1 / (double)5);
    public const double BaseAnimalEnergyPerUnitOfRadius = .001;
    public const int BaseDefendedDamagePerUnitOfRadius = 50;
    public const int BaseEatingSpeedPerUnitOfRadius = 1;
    public const int BaseEyesightRadius = 5;
    public const int BaseInflictedDamagePerUnitOfRadius = 50;
    public const int BasePlantEnergyPerUnitOfRadius = 1;
    public const double CarnivoreAttackDefendMultiplier = 2;
    public const int CarnivoreLifeSpanMultiplier = 2;
    public const int DamageToKillPerUnitOfRadius = 190;
    public const int EnergyPerAnimalFoodChunk = 1;
    public const int EnergyPerPlantFoodChunk = 1;

    private const double EnergyRequiredToMoveMinimumRequirements =
        MinimumUnitsToMoveAtMinimumEnergy * MinimumSpeedToMoveAtMinimumEnergy *
        RequiredEnergyPerUnitOfRadiusSpeedDistance +
        (MinimumUnitsToMoveAtMinimumEnergy / MinimumSpeedToMoveAtMinimumEnergy) *
        BaseAnimalEnergyPerUnitOfRadius;

    public const int FoodChunksPerUnitOfRadius = 25;
    public const int GridCellHeight = 1 << GridHeightPowerOfTwo;
    public const int GridCellWidth = 1 << GridWidthPowerOfTwo;
    public const int GridHeightPowerOfTwo = 3;
    public const int GridWidthPowerOfTwo = 3;
    public const int InvisibleOddsBase = 0;
    public const int InvisibleOddsMaximum = 90;
    public const int MaxAvailableCharacteristicPoints = 100;
    public const double MaxEnergyBasePerUnitRadius = (int)EnergyRequiredToMoveMinimumRequirements;
    public const int MaxEnergyFromLightPerTick = 550;
    public const double MaxEnergyMaximumPerUnitRadius = (int)(EnergyRequiredToMoveMinimumRequirements * 20);
    public const int MaxGridRadius = (MaxMatureSize / GridCellWidth / 2) + 1;
    public const int MaximumDefendedDamagePerUnitOfRadius = 25;
    public const int MaximumEatingSpeedPerUnitOfRadius = 100;
    public const int MaximumEyesightRadius = 10;
    public const int MaximumInflictedDamagePerUnitOfRadius = 25;
    public const int MaxMatureSize = 48;
    public const int MaxSeedSpreadDistance = 1000;

    private const double MinimumSpeedToMoveAtMinimumEnergy = 5;
    private const double MinimumUnitsToMoveAtMinimumEnergy = 2000;

    public const int MinMatureSize = 25;
    public const int NumberOfAnimalsPerTeleporter = 100;
    public const int OrganismSchedulingBlacklistOvertime = 500000;
    public const int OrganismSchedulingMaximumOvertime = 50000;
    public const int PlantFoodChunksPerUnitOfRadius = 50;
    public const double PlantIncubationEnergyMultiplier = 1.5;
    public const double PlantIncubationEnergyPerUnitOfRadius =
        (PlantMatureSizeProvidedEnergyPerUnitRadius / TicksToIncubate) *
        PlantIncubationEnergyMultiplier;
    public const int PlantLifeSpanPerUnitMaximumRadius = 150;
    public const double PlantMatureSizeProvidedEnergyPerUnitRadius =
        PlantFoodChunksPerUnitOfRadius * EnergyPerPlantFoodChunk;
    public const int PlantMaxHealingPerTickPerRadius = 1;
    public const int PlantReproductionWaitPerUnitRadius = 25;
    public const int PlantRequiredEnergyPerUnitOfHealing = 100;
    public const double PlantRequiredEnergyPerUnitOfRadiusGrowth = MaxEnergyBasePerUnitRadius * (1 / (double)5);
    public const double RequiredEnergyPerUnitOfRadiusSpeedDistance = .005;
    public const int SpeedBase = 5;
    public const int SpeedMaximum = 100;
    public const int TicksToIncubate = 10;
    public const int TimeToRot = 60;
    public const int ViewPortHeight = 450;
    public const int ViewPortWidth = 800;
    public const int MonitorModeHeight = 600;
    public const int MonitorModeWidth = 800;

    public static void EngineSettingsAsserts()
    {
        Debug.Assert(RequiredEnergyPerUnitOfRadiusSpeedDistance > BaseAnimalEnergyPerUnitOfRadius);
        Debug.Assert(PlantRequiredEnergyPerUnitOfRadiusGrowth <= ((MaxEnergyBasePerUnitRadius / 5) * 1));
        Debug.Assert(AnimalRequiredEnergyPerUnitOfRadiusGrowth <= ((MaxEnergyBasePerUnitRadius / 5) * 1));
        Debug.Assert(AnimalIncubationEnergyPerUnitOfRadius <= ((MaxEnergyBasePerUnitRadius / 5) * 1));
        Debug.Assert(PlantIncubationEnergyPerUnitOfRadius <= ((MaxEnergyBasePerUnitRadius / 5) * 1));
        Debug.Assert((BasePlantEnergyPerUnitOfRadius * MaxMatureSize) < (MaxEnergyFromLightPerTick / (float)2));
        Debug.Assert((float)BaseAnimalEnergyPerUnitOfRadius * MaxMatureSize <
                     EnergyPerPlantFoodChunk * (float)BaseEatingSpeedPerUnitOfRadius);
        Debug.Assert(((AnimalLifeSpanPerUnitMaximumRadius * MaxMatureSize) / (float)2) >=
                     3 * ((float)AnimalReproductionWaitPerUnitRadius * MaxMatureSize));
        Debug.Assert(((PlantLifeSpanPerUnitMaximumRadius * MaxMatureSize) / (float)2) >=
                     3 * ((float)PlantReproductionWaitPerUnitRadius * MaxMatureSize));
        Debug.Assert(OrganismSchedulingMaximumOvertime < OrganismSchedulingBlacklistOvertime);
        Debug.Assert((int)MaxEnergyBasePerUnitRadius > 0);
    }
}
