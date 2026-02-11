using System.Drawing;
using OrganismBase;
using Xunit;

namespace Terrarium.OrganismBase.Tests;

public class AnimalStateTests
{
    private static AnimalState CreateAnimalState(
        string id = "animal-1",
        EnergyState energy = EnergyState.Full,
        int radius = 10)
    {
        var species = new MockAnimalSpecies();
        return new AnimalState(id, species, 0, energy, radius);
    }

    [Fact]
    public void Constructor_SetsBasicProperties()
    {
        var species = new MockAnimalSpecies();
        var state = new AnimalState("a1", species, 2, EnergyState.Normal, 10);

        Assert.Equal("a1", state.ID);
        Assert.Equal(2, state.Generation);
        Assert.Equal(10, state.Radius);
        Assert.Same(species, state.Species);
        Assert.True(state.IsAlive);
        Assert.Equal(0, state.Damage);
    }

    [Fact]
    public void AnimalSpecies_ReturnsTypedSpecies()
    {
        var state = CreateAnimalState();
        Assert.IsType<MockAnimalSpecies>(state.AnimalSpecies);
        Assert.True(state.AnimalSpecies.IsCarnivore);
    }

    [Fact]
    public void IsAlive_InitiallyTrue()
    {
        var state = CreateAnimalState();
        Assert.True(state.IsAlive);
    }

    [Fact]
    public void Kill_SetsDeathReasonAndEnergy()
    {
        var state = CreateAnimalState();
        state.Kill(PopulationChangeReason.Killed);
        Assert.False(state.IsAlive);
        Assert.Equal(PopulationChangeReason.Killed, state.DeathReason);
        Assert.Equal(0, state.StoredEnergy);
    }

    [Fact]
    public void CauseDamage_AccumulatesDamage()
    {
        var state = CreateAnimalState();
        state.CauseDamage(50);
        Assert.Equal(50, state.Damage);
    }

    [Fact]
    public void CauseDamage_ExceedingThreshold_KillsOrganism()
    {
        var state = CreateAnimalState(radius: 10);
        int lethalDamage = EngineSettings.DamageToKillPerUnitOfRadius * 10;
        state.CauseDamage(lethalDamage);
        Assert.False(state.IsAlive);
        Assert.Equal(PopulationChangeReason.Killed, state.DeathReason);
    }

    [Fact]
    public void CauseDamage_NegativeValue_Throws()
    {
        var state = CreateAnimalState();
        Assert.Throws<GameEngineException>(() => state.CauseDamage(-1));
    }

    [Fact]
    public void PercentInjured_NoDamage_ReturnsZero()
    {
        var state = CreateAnimalState();
        Assert.Equal(0.0, state.PercentInjured);
    }

    [Fact]
    public void PercentInjured_WithDamage_ReturnsPercentage()
    {
        var state = CreateAnimalState(radius: 10);
        int maxDamage = EngineSettings.DamageToKillPerUnitOfRadius * 10;
        state.CauseDamage(maxDamage / 2);
        Assert.True(state.PercentInjured > 0);
        Assert.True(state.PercentInjured < 1.0);
    }

    [Fact]
    public void HealDamage_WithEnoughEnergy_ReducesDamage()
    {
        var state = CreateAnimalState(energy: EnergyState.Full, radius: 10);
        state.CauseDamage(100);
        var damageBefore = state.Damage;
        state.HealDamage();
        Assert.True(state.Damage <= damageBefore);
    }

    [Fact]
    public void EnergyRequiredToMove_ReturnsExpectedValue()
    {
        var state = CreateAnimalState(radius: 10);
        double energy = state.EnergyRequiredToMove(100.0, 5);
        double expected = 100.0 * 10 * 5 * EngineSettings.RequiredEnergyPerUnitOfRadiusSpeedDistance;
        Assert.Equal(expected, energy);
    }

    [Fact]
    public void AddRotTick_IncreasesRotTicks()
    {
        var state = CreateAnimalState();
        state.Kill(PopulationChangeReason.Killed);
        state.AddRotTick();
        Assert.Equal(1, state.RotTicks);
    }

    [Fact]
    public void IncreaseRadiusTo_IncreasesRadiusAndFoodChunks()
    {
        var state = CreateAnimalState(radius: 5);
        int foodBefore = state.FoodChunks;
        state.IncreaseRadiusTo(7);
        Assert.Equal(7, state.Radius);
        Assert.True(state.FoodChunks > foodBefore);
    }

    [Fact]
    public void CloneMutable_CreatesMutableCopy()
    {
        var state = CreateAnimalState();
        state.MakeImmutable();
        var clone = state.CloneMutable();
        Assert.False(clone.IsImmutable);
        Assert.Equal(state.ID, clone.ID);
    }

    [Fact]
    public void PreviousDisplayAction_JustDied_ReturnsDied()
    {
        var state = CreateAnimalState();
        state.Kill(PopulationChangeReason.Killed);
        Assert.Equal(DisplayAction.Died, state.PreviousDisplayAction);
    }

    [Fact]
    public void Antennas_DefaultState_HasDefaultPositions()
    {
        var state = CreateAnimalState();
        Assert.NotNull(state.Antennas);
        Assert.Equal(AntennaPosition.Left, state.Antennas.LeftAntenna);
    }

    [Fact]
    public void Antennas_CanSetOnMutableState()
    {
        var state = CreateAnimalState();
        var antennas = new AntennaState(AntennaPosition.Top, AntennaPosition.Bottom);
        state.Antennas = antennas;
        Assert.Equal(AntennaPosition.Top, state.Antennas.LeftAntenna);
    }

    [Fact]
    public void Antennas_SetOnImmutable_Throws()
    {
        var state = CreateAnimalState();
        state.MakeImmutable();
        var antennas = new AntennaState(AntennaPosition.Top, AntennaPosition.Bottom);
        Assert.Throws<GameEngineException>(() => state.Antennas = antennas);
    }
}
