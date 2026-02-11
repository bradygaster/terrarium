using System.Drawing;
using OrganismBase;
using Xunit;

namespace Terrarium.Game.Tests;

/// <summary>
/// Tests for WorldState: grid creation, organism placement,
/// cell queries, immutability, and index operations.
/// </summary>
public class WorldStateTests
{
    private static readonly IAnimalSpecies AnimalSpecies = new MockAnimalSpecies();
    private static readonly IPlantSpecies PlantSpecies = new MockPlantSpecies();

    private static AnimalState CreateAnimalAt(string id, int x, int y, int radius = 2)
    {
        var state = new AnimalState(id, AnimalSpecies, 0, EnergyState.Full, radius);
        state.Position = new Point(
            x << EngineSettings.GridWidthPowerOfTwo,
            y << EngineSettings.GridHeightPowerOfTwo);
        return state;
    }

    private static PlantState CreatePlantAt(string id, int x, int y, int radius = 2)
    {
        var state = new PlantState(id, PlantSpecies, 0, EnergyState.Full, radius);
        state.Position = new Point(
            x << EngineSettings.GridWidthPowerOfTwo,
            y << EngineSettings.GridHeightPowerOfTwo);
        return state;
    }

    // --- Grid creation ---

    [Fact]
    public void WorldState_CanBeCreated_WithDimensions()
    {
        var ws = new WorldState(100, 100);
        Assert.NotNull(ws);
        Assert.False(ws.IsImmutable);
        Assert.False(ws.IndexBuilt);
    }

    [Fact]
    public void WorldState_StartsEmpty()
    {
        var ws = new WorldState(50, 50);
        Assert.Empty(ws.Organisms);
        Assert.Empty(ws.OrganismIDs);
    }

    // --- Organism placement ---

    [Fact]
    public void AddOrganism_IncreasesCount()
    {
        var ws = new WorldState(100, 100);
        var animal = CreateAnimalAt("a1", 10, 10);
        ws.AddOrganism(animal);

        Assert.Single(ws.Organisms);
        Assert.Contains("a1", ws.OrganismIDs);
    }

    [Fact]
    public void AddOrganism_MultiplePlacement()
    {
        var ws = new WorldState(100, 100);
        ws.AddOrganism(CreateAnimalAt("a1", 10, 10));
        ws.AddOrganism(CreatePlantAt("p1", 20, 20));

        Assert.Equal(2, ws.Organisms.Count);
    }

    [Fact]
    public void AddOrganism_DuplicateId_Throws()
    {
        var ws = new WorldState(100, 100);
        ws.AddOrganism(CreateAnimalAt("a1", 10, 10));

        Assert.Throws<InvalidOperationException>(() =>
            ws.AddOrganism(CreateAnimalAt("a1", 20, 20)));
    }

    // --- Cell queries ---

    [Fact]
    public void GetOrganismState_ReturnsPlacedOrganism()
    {
        var ws = new WorldState(100, 100);
        var animal = CreateAnimalAt("a1", 10, 10);
        ws.AddOrganism(animal);

        var retrieved = ws.GetOrganismState("a1");
        Assert.NotNull(retrieved);
        Assert.Equal("a1", retrieved!.ID);
    }

    [Fact]
    public void GetOrganismState_ReturnsNull_ForMissing()
    {
        var ws = new WorldState(100, 100);
        Assert.Null(ws.GetOrganismState("nonexistent"));
    }

    [Fact]
    public void IsGridCellOccupied_ReturnsTrue_ForOccupied()
    {
        // AddOrganism fills cells but doesn't set IndexBuilt.
        // Use DuplicateMutable + BuildIndex to set up a properly indexed state.
        var ws = new WorldState(100, 100);
        var animal = CreateAnimalAt("a1", 10, 10);
        ws.AddOrganism(animal);
        ws.MakeImmutable();

        var dup = ws.DuplicateMutable();
        dup.BuildIndex();

        Assert.True(dup.IsGridCellOccupied(10, 10));
    }

    [Fact]
    public void IsGridCellOccupied_ReturnsFalse_ForEmpty()
    {
        var ws = new WorldState(100, 100);
        ws.BuildIndex();

        Assert.False(ws.IsGridCellOccupied(10, 10));
    }

    // --- Remove ---

    [Fact]
    public void RemoveOrganism_RemovesFromState()
    {
        var ws = new WorldState(100, 100);
        ws.AddOrganism(CreateAnimalAt("a1", 10, 10));
        ws.RemoveOrganism("a1");

        Assert.Empty(ws.Organisms);
        Assert.Null(ws.GetOrganismState("a1"));
    }

    // --- Immutability ---

    [Fact]
    public void MakeImmutable_SetsFlag()
    {
        var ws = new WorldState(100, 100);
        ws.MakeImmutable();
        Assert.True(ws.IsImmutable);
    }

    [Fact]
    public void MakeImmutable_PreventsAddOrganism()
    {
        var ws = new WorldState(100, 100);
        ws.MakeImmutable();

        Assert.Throws<InvalidOperationException>(() =>
            ws.AddOrganism(CreateAnimalAt("a1", 10, 10)));
    }

    [Fact]
    public void MakeImmutable_PreventsRemoveOrganism()
    {
        var ws = new WorldState(100, 100);
        ws.AddOrganism(CreateAnimalAt("a1", 10, 10));
        ws.MakeImmutable();

        Assert.Throws<InvalidOperationException>(() => ws.RemoveOrganism("a1"));
    }

    [Fact]
    public void MakeImmutable_PreventsTickNumberChange()
    {
        var ws = new WorldState(100, 100);
        ws.MakeImmutable();

        Assert.Throws<InvalidOperationException>(() => ws.TickNumber = 5);
    }

    [Fact]
    public void MakeImmutable_PreventsStateGuidChange()
    {
        var ws = new WorldState(100, 100);
        ws.MakeImmutable();

        Assert.Throws<InvalidOperationException>(() => ws.StateGuid = Guid.NewGuid());
    }

    // --- DuplicateMutable ---

    [Fact]
    public void DuplicateMutable_CreatesModifiableCopy()
    {
        var ws = new WorldState(100, 100);
        ws.AddOrganism(CreateAnimalAt("a1", 10, 10));
        ws.TickNumber = 42;
        ws.MakeImmutable();

        var dup = ws.DuplicateMutable();
        Assert.False(dup.IsImmutable);
        Assert.Equal(42, dup.TickNumber);
        Assert.NotNull(dup.GetOrganismState("a1"));
    }

    // --- TickNumber and StateGuid ---

    [Fact]
    public void TickNumber_CanBeSetAndRead()
    {
        var ws = new WorldState(100, 100);
        ws.TickNumber = 99;
        Assert.Equal(99, ws.TickNumber);
    }

    [Fact]
    public void StateGuid_CanBeSetAndRead()
    {
        var ws = new WorldState(100, 100);
        var guid = Guid.NewGuid();
        ws.StateGuid = guid;
        Assert.Equal(guid, ws.StateGuid);
    }

    // --- ZOrderedOrganisms ---

    [Fact]
    public void ZOrderedOrganisms_ReturnsSortedList()
    {
        var ws = new WorldState(100, 100);
        ws.AddOrganism(CreateAnimalAt("a1", 10, 10));
        ws.AddOrganism(CreatePlantAt("p1", 20, 20));

        var ordered = ws.ZOrderedOrganisms;
        Assert.Equal(2, ordered.Count);
    }

    // --- BuildIndex ---

    [Fact]
    public void BuildIndex_SetsIndexBuiltFlag()
    {
        var ws = new WorldState(100, 100);
        ws.BuildIndex();
        Assert.True(ws.IndexBuilt);
    }

    [Fact]
    public void ClearIndex_ResetsFlag()
    {
        var ws = new WorldState(100, 100);
        ws.BuildIndex();
        ws.ClearIndex();
        Assert.False(ws.IndexBuilt);
    }

    // --- FindOrganismsInCells ---

    [Fact]
    public void FindOrganismsInCells_FindsPlacedOrganism()
    {
        var ws = new WorldState(100, 100);
        ws.AddOrganism(CreateAnimalAt("a1", 10, 10));
        ws.MakeImmutable();

        var dup = ws.DuplicateMutable();
        dup.BuildIndex();

        var found = dup.FindOrganismsInCells(8, 12, 8, 12);
        Assert.Single(found);
        Assert.Equal("a1", found[0].ID);
    }

    [Fact]
    public void FindOrganismsInCells_ReturnsEmpty_WhenNoneInRange()
    {
        var ws = new WorldState(100, 100);
        ws.AddOrganism(CreateAnimalAt("a1", 10, 10));
        ws.MakeImmutable();

        var dup = ws.DuplicateMutable();
        dup.BuildIndex();

        var found = dup.FindOrganismsInCells(50, 60, 50, 60);
        Assert.Empty(found);
    }

    // --- Mock species ---

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
