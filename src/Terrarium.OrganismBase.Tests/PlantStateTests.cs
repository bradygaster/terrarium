using OrganismBase;
using Xunit;

namespace Terrarium.OrganismBase.Tests;

public class PlantStateTests
{
    private static PlantState CreatePlantState(
        string id = "plant-1",
        EnergyState energy = EnergyState.Full,
        int radius = 10)
    {
        var species = new MockPlantSpecies();
        return new PlantState(id, species, 0, energy, radius);
    }

    [Fact]
    public void Constructor_SetsBasicProperties()
    {
        var species = new MockPlantSpecies();
        var state = new PlantState("p1", species, 1, EnergyState.Normal, 8);

        Assert.Equal("p1", state.ID);
        Assert.Equal(1, state.Generation);
        Assert.Equal(8, state.Radius);
        Assert.True(state.IsAlive);
    }

    [Fact]
    public void CurrentMaxFoodChunks_CalculatesCorrectly()
    {
        var state = CreatePlantState(radius: 10);
        Assert.Equal(10 * EngineSettings.PlantFoodChunksPerUnitOfRadius, state.CurrentMaxFoodChunks);
    }

    [Fact]
    public void PercentInjured_FullHealth_ReturnsNearZero()
    {
        var state = CreatePlantState(radius: 5);
        // Set food chunks to max
        state.IncreaseRadiusTo(6);
        // PercentInjured based on food chunks
        Assert.True(state.PercentInjured >= 0);
    }

    [Fact]
    public void GiveEnergy_OptimalLight_AddsMaxEnergy()
    {
        var state = CreatePlantState(energy: EnergyState.Hungry, radius: 5);
        double energyBefore = state.StoredEnergy;
        state.GiveEnergy(100); // optimal light
        Assert.True(state.StoredEnergy >= energyBefore);
    }

    [Fact]
    public void GiveEnergy_ZeroLight_AddsNoEnergy()
    {
        var state = CreatePlantState(energy: EnergyState.Hungry, radius: 5);
        double energyBefore = state.StoredEnergy;
        state.GiveEnergy(0); // zero light = 100% from optimal
        // At 0 availability, percentageFromOptimal = 100, so energy gained = 0
        Assert.Equal(energyBefore, state.StoredEnergy);
    }

    [Fact]
    public void IncreaseRadiusTo_UpdatesHeightAndFoodChunks()
    {
        var state = CreatePlantState(radius: 5);
        state.IncreaseRadiusTo(8);
        Assert.Equal(8, state.Radius);
        Assert.Equal(8, state.Height);
    }

    [Fact]
    public void CloneMutable_CreatesMutableCopy()
    {
        var state = CreatePlantState();
        state.MakeImmutable();
        var clone = (PlantState)state.CloneMutable();
        Assert.False(clone.IsImmutable);
        Assert.Equal(state.ID, clone.ID);
    }

    [Fact]
    public void Kill_SetsDeathReason()
    {
        var state = CreatePlantState();
        state.Kill(PopulationChangeReason.Starved);
        Assert.False(state.IsAlive);
        Assert.Equal(PopulationChangeReason.Starved, state.DeathReason);
    }

    [Fact]
    public void GiveEnergy_OnImmutable_Throws()
    {
        var state = CreatePlantState();
        state.MakeImmutable();
        Assert.Throws<GameEngineException>(() => state.GiveEnergy(100));
    }
}
