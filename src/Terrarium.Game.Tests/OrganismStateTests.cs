using System.Drawing;
using OrganismBase;
using Xunit;

namespace Terrarium.Game.Tests;

/// <summary>
/// Tests that OrganismState (via AnimalState/PlantState) can be created,
/// properties set, and position tracked — validating the types the game engine uses.
/// </summary>
public class OrganismStateTests
{
    private static readonly MockAnimalSpecies DefaultAnimalSpecies = new();
    private static readonly MockPlantSpecies DefaultPlantSpecies = new();

    private static AnimalState CreateAnimalState(
        string id = "animal-1",
        EnergyState energy = EnergyState.Full,
        int radius = 10)
    {
        return new AnimalState(id, DefaultAnimalSpecies, 0, energy, radius);
    }

    private static PlantState CreatePlantState(
        string id = "plant-1",
        EnergyState energy = EnergyState.Full,
        int radius = 10)
    {
        return new PlantState(id, DefaultPlantSpecies, 0, energy, radius);
    }

    // --- Creation ---

    [Fact]
    public void AnimalState_CanBeCreated_WithValidParameters()
    {
        var state = CreateAnimalState();
        Assert.NotNull(state);
        Assert.Equal("animal-1", state.ID);
        Assert.True(state.IsAlive);
    }

    [Fact]
    public void PlantState_CanBeCreated_WithValidParameters()
    {
        var state = CreatePlantState();
        Assert.NotNull(state);
        Assert.Equal("plant-1", state.ID);
        Assert.True(state.IsAlive);
    }

    [Fact]
    public void AnimalState_InitialGeneration_IsZero()
    {
        var state = CreateAnimalState();
        Assert.Equal(0, state.Generation);
    }

    [Fact]
    public void AnimalState_Species_IsSet()
    {
        var state = CreateAnimalState();
        Assert.Same(DefaultAnimalSpecies, state.Species);
    }

    // --- Position Tracking ---

    [Fact]
    public void Position_DefaultIsOrigin()
    {
        var state = CreateAnimalState();
        Assert.Equal(new Point(0, 0), state.Position);
    }

    [Fact]
    public void Position_CanBeSetAndRead()
    {
        var state = CreateAnimalState();
        state.Position = new Point(150, 250);
        Assert.Equal(new Point(150, 250), state.Position);
    }

    [Fact]
    public void Position_ReturnsNewPointEachTime()
    {
        var state = CreateAnimalState();
        state.Position = new Point(10, 20);
        var p1 = state.Position;
        var p2 = state.Position;
        Assert.Equal(p1, p2);
    }

    [Fact]
    public void Position_MultipleUpdates_TracksLatest()
    {
        var state = CreateAnimalState();
        state.Position = new Point(10, 20);
        state.Position = new Point(30, 40);
        state.Position = new Point(50, 60);
        Assert.Equal(new Point(50, 60), state.Position);
    }

    // --- Energy ---

    [Fact]
    public void StoredEnergy_FullState_MatchesUpperBoundary()
    {
        var species = new MockAnimalSpecies();
        var state = new AnimalState("e1", species, 0, EnergyState.Full, 10);
        double expected = OrganismState.UpperBoundaryForEnergyState(species, EnergyState.Full, 10);
        Assert.Equal(expected, state.StoredEnergy);
    }

    [Fact]
    public void EnergyState_ReflectsStoredEnergy()
    {
        var state = CreateAnimalState(energy: EnergyState.Full);
        Assert.Equal(EnergyState.Full, state.EnergyState);
    }

    [Fact]
    public void BurnEnergy_ReducesEnergy()
    {
        var state = CreateAnimalState(energy: EnergyState.Full);
        double before = state.StoredEnergy;
        state.BurnEnergy(1.0);
        Assert.True(state.StoredEnergy < before);
    }

    [Fact]
    public void Kill_SetsDeathReasonAndStopsAlive()
    {
        var state = CreateAnimalState();
        state.Kill(PopulationChangeReason.Killed);
        Assert.False(state.IsAlive);
        Assert.Equal(PopulationChangeReason.Killed, state.DeathReason);
    }

    // --- Immutability ---

    [Fact]
    public void MakeImmutable_PreventsPositionChange()
    {
        var state = CreateAnimalState();
        state.MakeImmutable();
        Assert.True(state.IsImmutable);
        Assert.Throws<GameEngineException>(() => state.Position = new Point(1, 1));
    }

    [Fact]
    public void MakeImmutable_PreventsEnergyChange()
    {
        var state = CreateAnimalState();
        state.MakeImmutable();
        Assert.Throws<GameEngineException>(() => state.StoredEnergy = 100);
    }

    // --- CloneMutable ---

    [Fact]
    public void CloneMutable_CreatesModifiableCopy()
    {
        var state = CreateAnimalState();
        state.Position = new Point(42, 84);
        state.MakeImmutable();

        var clone = state.CloneMutable();
        Assert.False(clone.IsImmutable);
        Assert.Equal(state.ID, clone.ID);
    }

    [Fact]
    public void PlantState_CloneMutable_PreservesProperties()
    {
        var state = CreatePlantState();
        state.Position = new Point(100, 200);
        state.MakeImmutable();

        var clone = (PlantState)state.CloneMutable();
        Assert.False(clone.IsImmutable);
        Assert.Equal("plant-1", clone.ID);
    }

    // --- Aging ---

    [Fact]
    public void AddTickToAge_Increments()
    {
        var state = CreateAnimalState();
        Assert.Equal(0, state.TickAge);
        state.AddTickToAge();
        Assert.Equal(1, state.TickAge);
    }

    [Fact]
    public void PercentLifespanRemaining_AtBirth_IsOne()
    {
        var state = CreateAnimalState();
        Assert.Equal(1.0, state.PercentLifespanRemaining);
    }

    // --- Adjacency ---

    [Fact]
    public void IsAdjacentOrOverlapping_SamePosition_ReturnsTrue()
    {
        var s1 = CreateAnimalState(id: "a");
        var s2 = CreateAnimalState(id: "b");
        s1.Position = new Point(100, 100);
        s2.Position = new Point(100, 100);
        Assert.True(s1.IsAdjacentOrOverlapping(s2));
    }

    [Fact]
    public void IsWithinRect_NullState_ReturnsFalse()
    {
        var state = CreateAnimalState();
        Assert.False(state.IsWithinRect(0, null));
    }

    // --- Mock species helpers ---

    private class MockAnimalSpecies : IAnimalSpecies
    {
        public int MatureRadius { get; set; } = 12;
        public int ReproductionWait { get; set; } = 8;
        public int LifeSpan { get; set; } = 1000;
        public int GrowthWait { get; set; } = 5;
        public int MaximumEnergyPerUnitRadius { get; set; } = 50;
        public string Skin { get; set; } = "TestSkin";
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

    private class MockPlantSpecies : IPlantSpecies
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
}
