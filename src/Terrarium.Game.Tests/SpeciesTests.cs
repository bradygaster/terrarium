using System.Reflection;
using OrganismBase;
using Xunit;

namespace Terrarium.Game.Tests;

/// <summary>
/// Tests for Species: attribute reading, assembly parsing, and species creation.
/// Uses sample assemblies (SimpleCarnivore, SimpleHerbivore) for integration tests.
/// </summary>
public class SpeciesTests
{
    // --- GetSpeciesFromAssembly ---

    [Fact]
    public void GetSpeciesFromAssembly_SimpleCarnivore_ReturnsAnimalSpecies()
    {
        var assembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var species = Species.GetSpeciesFromAssembly(assembly);

        Assert.NotNull(species);
        Assert.IsType<AnimalSpecies>(species);
    }

    [Fact]
    public void GetSpeciesFromAssembly_SimpleCarnivore_HasAuthorName()
    {
        var assembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var species = Species.GetSpeciesFromAssembly(assembly);

        Assert.False(string.IsNullOrEmpty(species.AuthorName));
    }

    [Fact]
    public void GetSpeciesFromAssembly_SimpleCarnivore_HasMatureRadius()
    {
        var assembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var species = Species.GetSpeciesFromAssembly(assembly);

        Assert.True(species.MatureRadius > 0);
    }

    [Fact]
    public void GetSpeciesFromAssembly_SimpleCarnivore_HasMaxEnergy()
    {
        var assembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var species = Species.GetSpeciesFromAssembly(assembly);

        Assert.True(species.MaximumEnergyPerUnitRadius >= 0);
    }

    [Fact]
    public void GetSpeciesFromAssembly_SimpleHerbivore_ReturnsAnimalSpecies()
    {
        var assembly = typeof(Terrarium.Samples.SimpleHerbivore.SimpleHerbivore).Assembly;
        var species = Species.GetSpeciesFromAssembly(assembly);

        Assert.NotNull(species);
        Assert.IsType<AnimalSpecies>(species);
    }

    [Fact]
    public void GetSpeciesFromAssembly_SimpleHerbivore_HasName()
    {
        var assembly = typeof(Terrarium.Samples.SimpleHerbivore.SimpleHerbivore).Assembly;
        var species = Species.GetSpeciesFromAssembly(assembly);

        Assert.False(string.IsNullOrEmpty(species.Name));
    }

    // --- AnimalSpecies-specific attributes ---

    [Fact]
    public void AnimalSpecies_SimpleCarnivore_IsCarnivore()
    {
        var assembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var species = (AnimalSpecies)Species.GetSpeciesFromAssembly(assembly);

        Assert.True(species.IsCarnivore);
    }

    [Fact]
    public void AnimalSpecies_SimpleHerbivore_IsNotCarnivore()
    {
        var assembly = typeof(Terrarium.Samples.SimpleHerbivore.SimpleHerbivore).Assembly;
        var species = (AnimalSpecies)Species.GetSpeciesFromAssembly(assembly);

        Assert.False(species.IsCarnivore);
    }

    [Fact]
    public void AnimalSpecies_HasMaximumSpeed()
    {
        var assembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var species = (AnimalSpecies)Species.GetSpeciesFromAssembly(assembly);

        Assert.True(species.MaximumSpeed > 0);
    }

    [Fact]
    public void AnimalSpecies_HasEyesightRadius()
    {
        var assembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var species = (AnimalSpecies)Species.GetSpeciesFromAssembly(assembly);

        Assert.True(species.EyesightRadius > 0);
    }

    // --- Species properties ---

    [Fact]
    public void Species_InitialRadius_IsLessThanMatureRadius()
    {
        var assembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var species = Species.GetSpeciesFromAssembly(assembly);

        Assert.True(species.InitialRadius < species.MatureRadius);
    }

    [Fact]
    public void Species_LifeSpan_IsPositive()
    {
        var assembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var species = Species.GetSpeciesFromAssembly(assembly);

        Assert.True(species.LifeSpan > 0);
    }

    [Fact]
    public void Species_ReproductionWait_IsPositive()
    {
        var assembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var species = Species.GetSpeciesFromAssembly(assembly);

        Assert.True(species.ReproductionWait > 0);
    }

    [Fact]
    public void Species_GrowthWait_IsPositive()
    {
        var assembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var species = Species.GetSpeciesFromAssembly(assembly);

        Assert.True(species.GrowthWait > 0);
    }

    [Fact]
    public void Species_AssemblyFullName_IsNotEmpty()
    {
        var assembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var species = Species.GetSpeciesFromAssembly(assembly);

        Assert.False(string.IsNullOrEmpty(species.AssemblyFullName));
    }

    [Fact]
    public void Species_IsSameSpecies_MatchesSelf()
    {
        var assembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var species = Species.GetSpeciesFromAssembly(assembly);

        Assert.True(species.IsSameSpecies(species));
    }

    [Fact]
    public void Species_IsSameSpecies_DifferentSpecies_ReturnsFalse()
    {
        var carnivoreAssembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var herbivoreAssembly = typeof(Terrarium.Samples.SimpleHerbivore.SimpleHerbivore).Assembly;
        var carnivore = Species.GetSpeciesFromAssembly(carnivoreAssembly);
        var herbivore = Species.GetSpeciesFromAssembly(herbivoreAssembly);

        Assert.False(carnivore.IsSameSpecies(herbivore));
    }

    // --- GetAssemblyShortName ---

    [Fact]
    public void GetAssemblyShortName_ExtractsNameBeforeComma()
    {
        var result = typeof(Species)
            .GetMethod("GetAssemblyShortName", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { "MyAssembly, Version=1.0.0.0, Culture=neutral" });

        Assert.Equal("MyAssembly", result);
    }

    [Fact]
    public void GetAssemblyShortName_ReturnsFullString_WhenNoComma()
    {
        var result = typeof(Species)
            .GetMethod("GetAssemblyShortName", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { "SimpleAssemblyName" });

        Assert.Equal("SimpleAssemblyName", result);
    }
}
