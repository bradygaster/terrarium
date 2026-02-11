using System;
using System.Reflection;
using OrganismBase;
using Xunit;

namespace Terrarium.Game.Tests;

/// <summary>
/// Tests that creature attributes (MaximumEnergyPoints, MatureSize, etc.)
/// can be read from the sample creatures via reflection, validating the
/// attribute system the game engine relies on.
/// </summary>
public class SpeciesAttributeTests
{
    private static T GetAttribute<T>(Type type) where T : Attribute
    {
        var attr = type.GetCustomAttribute<T>();
        Assert.NotNull(attr);
        return attr!;
    }

    // --- SimpleCarnivore attribute tests ---

    [Fact]
    public void SimpleCarnivore_HasMatureSizeAttribute()
    {
        var type = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore);
        var attr = GetAttribute<MatureSizeAttribute>(type);
        Assert.Equal(15, attr.MatureRadius); // MatureSize(30) → radius = 30/2
    }

    [Fact]
    public void SimpleCarnivore_IsCarnivore()
    {
        var type = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore);
        var attr = GetAttribute<CarnivoreAttribute>(type);
        Assert.True(attr.IsCarnivore);
    }

    [Fact]
    public void SimpleCarnivore_HasAttackDamagePoints()
    {
        var type = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore);
        var attr = GetAttribute<AttackDamagePointsAttribute>(type);
        Assert.Equal(52, attr.Points);
    }

    [Fact]
    public void SimpleCarnivore_HasMaximumSpeedPoints()
    {
        var type = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore);
        var attr = GetAttribute<MaximumSpeedPointsAttribute>(type);
        Assert.Equal(28, attr.Points);
    }

    [Fact]
    public void SimpleCarnivore_HasEyesightPoints()
    {
        var type = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore);
        var attr = GetAttribute<EyesightPointsAttribute>(type);
        Assert.Equal(20, attr.Points);
    }

    [Fact]
    public void SimpleCarnivore_HasMaximumEnergyPointsOfZero()
    {
        var type = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore);
        var attr = GetAttribute<MaximumEnergyPointsAttribute>(type);
        Assert.Equal(0, attr.Points);
    }

    [Fact]
    public void SimpleCarnivore_HasScorpionSkin()
    {
        var type = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore);
        var attr = GetAttribute<AnimalSkinAttribute>(type);
        Assert.Equal(AnimalSkinFamily.Scorpion, attr.SkinFamily);
    }

    [Fact]
    public void SimpleCarnivore_TotalPoints_Equal100()
    {
        var type = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore);
        int total =
            GetAttribute<MaximumEnergyPointsAttribute>(type).Points +
            GetAttribute<EatingSpeedPointsAttribute>(type).Points +
            GetAttribute<AttackDamagePointsAttribute>(type).Points +
            GetAttribute<DefendDamagePointsAttribute>(type).Points +
            GetAttribute<MaximumSpeedPointsAttribute>(type).Points +
            GetAttribute<CamouflagePointsAttribute>(type).Points +
            GetAttribute<EyesightPointsAttribute>(type).Points;
        Assert.Equal(100, total);
    }

    // --- SimpleHerbivore attribute tests ---

    [Fact]
    public void SimpleHerbivore_IsNotCarnivore()
    {
        var type = typeof(Terrarium.Samples.SimpleHerbivore.SimpleHerbivore);
        var attr = GetAttribute<CarnivoreAttribute>(type);
        Assert.False(attr.IsCarnivore);
    }

    [Fact]
    public void SimpleHerbivore_HasMatureSizeAttribute()
    {
        var type = typeof(Terrarium.Samples.SimpleHerbivore.SimpleHerbivore);
        var attr = GetAttribute<MatureSizeAttribute>(type);
        Assert.Equal(13, attr.MatureRadius); // MatureSize(26) → radius = 26/2
    }

    [Fact]
    public void SimpleHerbivore_HasHighCamouflage()
    {
        var type = typeof(Terrarium.Samples.SimpleHerbivore.SimpleHerbivore);
        var attr = GetAttribute<CamouflagePointsAttribute>(type);
        Assert.Equal(50, attr.Points);
    }

    [Fact]
    public void SimpleHerbivore_HasHighEyesight()
    {
        var type = typeof(Terrarium.Samples.SimpleHerbivore.SimpleHerbivore);
        var attr = GetAttribute<EyesightPointsAttribute>(type);
        Assert.Equal(50, attr.Points);
    }

    [Fact]
    public void SimpleHerbivore_TotalPoints_Equal100()
    {
        var type = typeof(Terrarium.Samples.SimpleHerbivore.SimpleHerbivore);
        int total =
            GetAttribute<MaximumEnergyPointsAttribute>(type).Points +
            GetAttribute<EatingSpeedPointsAttribute>(type).Points +
            GetAttribute<AttackDamagePointsAttribute>(type).Points +
            GetAttribute<DefendDamagePointsAttribute>(type).Points +
            GetAttribute<MaximumSpeedPointsAttribute>(type).Points +
            GetAttribute<CamouflagePointsAttribute>(type).Points +
            GetAttribute<EyesightPointsAttribute>(type).Points;
        Assert.Equal(100, total);
    }

    // --- SimplePlant attribute tests ---

    [Fact]
    public void SimplePlant_MatureSize_ThrowsDueToOutOfRange()
    {
        // SimplePlant uses MatureSize(24) which is below MinMatureSize(25).
        // The attribute constructor validates range, so reading it via reflection
        // triggers SizeOutOfRangeCharacteristicException. This verifies validation works.
        var type = typeof(Terrarium.Samples.SimplePlant.SimplePlant);
        Assert.Throws<SizeOutOfRangeCharacteristicException>(
            () => type.GetCustomAttribute<MatureSizeAttribute>());
    }

    [Fact]
    public void SimplePlant_HasPlantSkin()
    {
        var type = typeof(Terrarium.Samples.SimplePlant.SimplePlant);
        var attr = GetAttribute<PlantSkinAttribute>(type);
        Assert.Equal(PlantSkinFamily.Plant, attr.SkinFamily);
    }

    [Fact]
    public void SimplePlant_HasSeedSpreadDistance()
    {
        var type = typeof(Terrarium.Samples.SimplePlant.SimplePlant);
        var attr = GetAttribute<SeedSpreadDistanceAttribute>(type);
        Assert.Equal(0, attr.SeedSpreadDistance);
    }

    // --- Attribute validation tests ---

    [Fact]
    public void MatureSizeAttribute_WithinRange_Succeeds()
    {
        var attr = new MatureSizeAttribute(EngineSettings.MinMatureSize);
        Assert.Equal(EngineSettings.MinMatureSize / 2, attr.MatureRadius);
    }

    [Fact]
    public void MatureSizeAttribute_AboveMax_Throws()
    {
        Assert.Throws<SizeOutOfRangeCharacteristicException>(
            () => new MatureSizeAttribute(EngineSettings.MaxMatureSize + 1));
    }

    [Fact]
    public void MatureSizeAttribute_BelowMin_Throws()
    {
        Assert.Throws<SizeOutOfRangeCharacteristicException>(
            () => new MatureSizeAttribute(EngineSettings.MinMatureSize - 1));
    }

    [Fact]
    public void MaximumEnergyPointsAttribute_ExceedsMax_Throws()
    {
        Assert.Throws<TooManyPointsOnOneCharacteristicException>(
            () => new MaximumEnergyPointsAttribute(EngineSettings.MaxAvailableCharacteristicPoints + 1));
    }
}
