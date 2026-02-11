using System.Drawing;
using OrganismBase;

namespace Terrarium.OrganismBase.Tests;

/// <summary>
/// Mock IAnimalSpecies for testing AnimalState and related classes.
/// </summary>
internal class MockAnimalSpecies : IAnimalSpecies
{
    public int MatureRadius { get; set; } = 12;
    public int ReproductionWait { get; set; } = 8;
    public int LifeSpan { get; set; } = 1000;
    public int GrowthWait { get; set; } = 5;
    public int MaximumEnergyPerUnitRadius { get; set; } = 50;
    public string Skin { get; set; } = "TestAnimalSkin";
    public AnimalSkinFamily SkinFamily { get; set; } = AnimalSkinFamily.Ant;
    public bool IsCarnivore { get; set; } = true;
    public int EatingSpeedPerUnitRadius { get; set; } = 10;
    public int MaximumAttackDamagePerUnitRadius { get; set; } = 60;
    public int MaximumDefendDamagePerUnitRadius { get; set; } = 55;
    public int MaximumSpeed { get; set; } = 50;
    public int InvisibleOdds { get; set; } = 10;
    public int EyesightRadius { get; set; } = 8;

    public bool IsSameSpecies(ISpecies species) => species is MockAnimalSpecies;
}

/// <summary>
/// Mock IPlantSpecies for testing PlantState and related classes.
/// </summary>
internal class MockPlantSpecies : IPlantSpecies
{
    public int MatureRadius { get; set; } = 12;
    public int ReproductionWait { get; set; } = 25;
    public int LifeSpan { get; set; } = 2000;
    public int GrowthWait { get; set; } = 5;
    public int MaximumEnergyPerUnitRadius { get; set; } = 50;
    public string Skin { get; set; } = "TestPlantSkin";
    public PlantSkinFamily SkinFamily { get; set; } = PlantSkinFamily.Plant;

    public bool IsSameSpecies(ISpecies species) => species is MockPlantSpecies;
}
