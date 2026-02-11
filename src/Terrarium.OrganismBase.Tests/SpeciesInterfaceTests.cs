using OrganismBase;
using Xunit;

namespace Terrarium.OrganismBase.Tests;

public class SpeciesInterfaceTests
{
    [Fact]
    public void MockAnimalSpecies_ImplementsIAnimalSpecies()
    {
        IAnimalSpecies species = new MockAnimalSpecies();
        Assert.NotNull(species);
        Assert.True(species.MatureRadius > 0);
        Assert.True(species.LifeSpan > 0);
        Assert.True(species.MaximumEnergyPerUnitRadius > 0);
        Assert.NotNull(species.Skin);
    }

    [Fact]
    public void MockPlantSpecies_ImplementsIPlantSpecies()
    {
        IPlantSpecies species = new MockPlantSpecies();
        Assert.NotNull(species);
        Assert.True(species.MatureRadius > 0);
        Assert.True(species.LifeSpan > 0);
    }

    [Fact]
    public void IAnimalSpecies_HasAllProperties()
    {
        var species = new MockAnimalSpecies();
        Assert.Equal(AnimalSkinFamily.Ant, species.SkinFamily);
        Assert.True(species.IsCarnivore);
        Assert.True(species.EatingSpeedPerUnitRadius > 0);
        Assert.True(species.MaximumAttackDamagePerUnitRadius > 0);
        Assert.True(species.MaximumDefendDamagePerUnitRadius > 0);
        Assert.True(species.MaximumSpeed > 0);
        Assert.True(species.EyesightRadius > 0);
    }

    [Fact]
    public void IPlantSpecies_HasSkinFamily()
    {
        var species = new MockPlantSpecies();
        Assert.Equal(PlantSkinFamily.Plant, species.SkinFamily);
    }

    [Fact]
    public void IsSameSpecies_SameType_ReturnsTrue()
    {
        var species1 = new MockAnimalSpecies();
        var species2 = new MockAnimalSpecies();
        Assert.True(species1.IsSameSpecies(species2));
    }

    [Fact]
    public void IsSameSpecies_DifferentType_ReturnsFalse()
    {
        var animalSpecies = new MockAnimalSpecies();
        var plantSpecies = new MockPlantSpecies();
        Assert.False(animalSpecies.IsSameSpecies(plantSpecies));
    }

    [Fact]
    public void ISpecies_ReproductionWait_IsPositive()
    {
        ISpecies species = new MockAnimalSpecies();
        Assert.True(species.ReproductionWait > 0);
    }

    [Fact]
    public void ISpecies_GrowthWait_IsPositive()
    {
        ISpecies species = new MockAnimalSpecies();
        Assert.True(species.GrowthWait > 0);
    }
}
