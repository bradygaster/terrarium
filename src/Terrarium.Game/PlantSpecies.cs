// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Diagnostics;
using System.Drawing;
using System.Text;
using OrganismBase;

namespace Terrarium.Game;

/// <summary>
/// Holds all species information about a plant.
/// </summary>
public sealed class PlantSpecies : Species, IPlantSpecies
{
    public PlantSpecies(Type clrType) : base(clrType)
    {
        SkinFamily = PlantSkinFamily.Plant;
        Debug.Assert(clrType != null);
        Debug.Assert(typeof(Plant).IsAssignableFrom(clrType));

        var skinAttr = (PlantSkinAttribute?)Attribute.GetCustomAttribute(clrType, typeof(PlantSkinAttribute));
        if (skinAttr != null) { SkinFamily = skinAttr.SkinFamily; Skin = skinAttr.Skin; }

        var seedAttr = (SeedSpreadDistanceAttribute?)Attribute.GetCustomAttribute(clrType, typeof(SeedSpreadDistanceAttribute));
        if (seedAttr == null) throw new GameEngineException("SeedSpreadDistanceAttribute is required.");
        SeedSpreadDistance = seedAttr.SeedSpreadDistance;
    }

    public int SeedSpreadDistance { get; private set; }
    public PlantSkinFamily SkinFamily { get; private set; }
    public override int LifeSpan => MatureRadius * EngineSettings.PlantLifeSpanPerUnitMaximumRadius;
    public override int ReproductionWait => MatureRadius * EngineSettings.PlantReproductionWaitPerUnitRadius;

    public override string GetAttributeWarnings()
    {
        var w = new StringBuilder();
        w.Append(base.GetAttributeWarnings());
        return w.ToString();
    }

    public override OrganismState InitializeNewState(Point position, int generation)
    {
        var newState = new PlantState(Guid.NewGuid().ToString(), this, generation, EnergyState.Hungry, InitialRadius)
            { Position = position };
        newState.ResetGrowthWait();
        return newState;
    }
}
