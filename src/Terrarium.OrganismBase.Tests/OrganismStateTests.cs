using System.Drawing;
using OrganismBase;
using Xunit;

namespace Terrarium.OrganismBase.Tests;

public class OrganismStateTests
{
    private static AnimalState CreateState(
        EnergyState energy = EnergyState.Full,
        int radius = 10)
    {
        return new AnimalState("org-1", new MockAnimalSpecies(), 0, energy, radius);
    }

    // --- Energy ---

    [Fact]
    public void StoredEnergy_Initial_MatchesEnergyState()
    {
        var species = new MockAnimalSpecies();
        var state = new AnimalState("e1", species, 0, EnergyState.Full, 10);
        double expectedFull = OrganismState.UpperBoundaryForEnergyState(species, EnergyState.Full, 10);
        Assert.Equal(expectedFull, state.StoredEnergy);
    }

    [Fact]
    public void StoredEnergy_SetAboveMax_IsCapped()
    {
        var species = new MockAnimalSpecies();
        var state = new AnimalState("e2", species, 0, EnergyState.Normal, 10);
        double max = species.MaximumEnergyPerUnitRadius * 10.0;
        state.StoredEnergy = max * 2;
        Assert.Equal(max, state.StoredEnergy);
    }

    [Fact]
    public void StoredEnergy_SetToZero_KillsOrganism()
    {
        var state = CreateState();
        state.StoredEnergy = 0;
        Assert.False(state.IsAlive);
        Assert.Equal(PopulationChangeReason.Starved, state.DeathReason);
    }

    [Fact]
    public void StoredEnergy_SetOnImmutable_Throws()
    {
        var state = CreateState();
        state.MakeImmutable();
        Assert.Throws<GameEngineException>(() => state.StoredEnergy = 100);
    }

    [Fact]
    public void StoredEnergy_SetOnDead_Throws()
    {
        var state = CreateState();
        state.Kill(PopulationChangeReason.Killed);
        Assert.Throws<GameEngineException>(() => state.StoredEnergy = 100);
    }

    // --- EnergyState ---

    [Fact]
    public void EnergyState_Full_ReturnsFull()
    {
        var state = CreateState(EnergyState.Full);
        Assert.Equal(EnergyState.Full, state.EnergyState);
    }

    [Fact]
    public void EnergyState_Normal_ReturnsNormal()
    {
        var state = CreateState(EnergyState.Normal);
        Assert.Equal(EnergyState.Normal, state.EnergyState);
    }

    [Fact]
    public void EnergyState_Hungry_ReturnsHungry()
    {
        var state = CreateState(EnergyState.Hungry);
        Assert.Equal(EnergyState.Hungry, state.EnergyState);
    }

    // --- UpperBoundaryForEnergyState ---

    [Fact]
    public void UpperBoundary_Dead_ReturnsZero()
    {
        var species = new MockAnimalSpecies();
        Assert.Equal(0, OrganismState.UpperBoundaryForEnergyState(species, EnergyState.Dead, 10));
    }

    [Fact]
    public void UpperBoundary_Full_ReturnsMaxEnergy()
    {
        var species = new MockAnimalSpecies();
        double expected = species.MaximumEnergyPerUnitRadius * 10.0;
        Assert.Equal(expected, OrganismState.UpperBoundaryForEnergyState(species, EnergyState.Full, 10));
    }

    // --- BurnEnergy ---

    [Fact]
    public void BurnEnergy_ReducesStoredEnergy()
    {
        var state = CreateState(EnergyState.Full);
        double before = state.StoredEnergy;
        state.BurnEnergy(1.0);
        Assert.True(state.StoredEnergy < before);
    }

    [Fact]
    public void BurnEnergy_AllEnergy_KillsOrganism()
    {
        var state = CreateState(EnergyState.Full);
        state.BurnEnergy(state.StoredEnergy + 1);
        Assert.False(state.IsAlive);
        Assert.Equal(PopulationChangeReason.Starved, state.DeathReason);
    }

    // --- TickAge ---

    [Fact]
    public void AddTickToAge_IncrementsTick()
    {
        var state = CreateState();
        state.AddTickToAge();
        Assert.Equal(1, state.TickAge);
    }

    [Fact]
    public void AddTickToAge_ExceedsLifeSpan_KillsOrganism()
    {
        var species = new MockAnimalSpecies { LifeSpan = 3 };
        var state = new AnimalState("aging", species, 0, EnergyState.Full, 10);
        state.AddTickToAge();
        state.AddTickToAge();
        state.AddTickToAge();
        Assert.True(state.IsAlive);
        state.AddTickToAge(); // exceeds lifespan of 3
        Assert.False(state.IsAlive);
        Assert.Equal(PopulationChangeReason.OldAge, state.DeathReason);
    }

    [Fact]
    public void AddTickToAge_OnImmutable_Throws()
    {
        var state = CreateState();
        state.MakeImmutable();
        Assert.Throws<GameEngineException>(() => state.AddTickToAge());
    }

    // --- Growth ---

    [Fact]
    public void IsMature_WhenRadiusEqualsMatureRadius_ReturnsTrue()
    {
        var species = new MockAnimalSpecies { MatureRadius = 10 };
        var state = new AnimalState("mature", species, 0, EnergyState.Full, 10);
        Assert.True(state.IsMature);
    }

    [Fact]
    public void IsMature_WhenRadiusBelowMature_ReturnsFalse()
    {
        var species = new MockAnimalSpecies { MatureRadius = 12 };
        var state = new AnimalState("immature", species, 0, EnergyState.Full, 10);
        Assert.False(state.IsMature);
    }

    [Fact]
    public void IncreaseRadiusTo_IncreasesRadius()
    {
        var state = CreateState(radius: 5);
        state.IncreaseRadiusTo(7);
        Assert.Equal(7, state.Radius);
    }

    [Fact]
    public void IncreaseRadiusTo_SmallerRadius_Throws()
    {
        var state = CreateState(radius: 10);
        Assert.Throws<GameEngineException>(() => state.IncreaseRadiusTo(5));
    }

    [Fact]
    public void IncreaseRadiusTo_OnImmutable_Throws()
    {
        var state = CreateState(radius: 5);
        state.MakeImmutable();
        Assert.Throws<GameEngineException>(() => state.IncreaseRadiusTo(7));
    }

    // --- Position ---

    [Fact]
    public void Position_CanSetOnMutable()
    {
        var state = CreateState();
        state.Position = new Point(100, 200);
        Assert.Equal(new Point(100, 200), state.Position);
    }

    [Fact]
    public void Position_OnImmutable_Throws()
    {
        var state = CreateState();
        state.MakeImmutable();
        Assert.Throws<GameEngineException>(() => state.Position = new Point(100, 200));
    }

    [Fact]
    public void Position_OnLocked_Throws()
    {
        var state = CreateState();
        state.LockSizeAndPosition();
        Assert.Throws<GameEngineException>(() => state.Position = new Point(100, 200));
    }

    [Fact]
    public void Position_OnDead_Throws()
    {
        var state = CreateState();
        state.Kill(PopulationChangeReason.Killed);
        Assert.Throws<GameEngineException>(() => state.Position = new Point(100, 200));
    }

    // --- GridX/GridY ---

    [Fact]
    public void GridXY_CalculatesFromPosition()
    {
        var state = CreateState();
        state.Position = new Point(64, 32);
        Assert.Equal(64 >> EngineSettings.GridWidthPowerOfTwo, state.GridX);
        Assert.Equal(32 >> EngineSettings.GridHeightPowerOfTwo, state.GridY);
    }

    // --- Immutability ---

    [Fact]
    public void MakeImmutable_PreventsModification()
    {
        var state = CreateState();
        state.MakeImmutable();
        Assert.True(state.IsImmutable);
    }

    // --- FoodChunks ---

    [Fact]
    public void FoodChunks_SetToZeroOrLess_Throws()
    {
        var state = CreateState();
        Assert.Throws<GameEngineException>(() => state.FoodChunks = 0);
        Assert.Throws<GameEngineException>(() => state.FoodChunks = -1);
    }

    [Fact]
    public void FoodChunks_OnImmutable_Throws()
    {
        var state = CreateState();
        state.MakeImmutable();
        Assert.Throws<GameEngineException>(() => state.FoodChunks = 5);
    }

    // --- PercentEnergy ---

    [Fact]
    public void PercentEnergy_Full_ReturnsOne()
    {
        var state = CreateState(EnergyState.Full);
        Assert.True(state.PercentEnergy >= 0.99);
    }

    // --- PercentLifespanRemaining ---

    [Fact]
    public void PercentLifespanRemaining_AtBirth_ReturnsOne()
    {
        var state = CreateState();
        Assert.Equal(1.0, state.PercentLifespanRemaining);
    }

    // --- Reproduction ---

    [Fact]
    public void ReadyToReproduce_Initially_ReturnsTrue()
    {
        var state = CreateState();
        Assert.True(state.ReadyToReproduce);
    }

    [Fact]
    public void ResetReproductionWait_SetsWaitFromSpecies()
    {
        var state = CreateState();
        state.ResetReproductionWait();
        Assert.False(state.ReadyToReproduce);
    }

    [Fact]
    public void IsIncubating_InitiallyFalse()
    {
        var state = CreateState();
        Assert.False(state.IsIncubating);
    }

    [Fact]
    public void AddIncubationTick_IncrementsTicks()
    {
        var state = CreateState();
        state.AddIncubationTick();
        Assert.Equal(1, state.IncubationTicks);
    }

    // --- CompareTo ---

    [Fact]
    public void CompareTo_ComparesByYPosition()
    {
        var state1 = CreateState();
        state1.Position = new Point(0, 10);
        var state2 = CreateState();
        state2.Position = new Point(0, 20);
        Assert.True(state1.CompareTo(state2) < 0);
    }

    [Fact]
    public void CompareTo_NonOrganismState_ReturnsZero()
    {
        var state = CreateState();
        Assert.Equal(0, state.CompareTo("not a state"));
    }

    // --- IsAdjacentOrOverlapping ---

    [Fact]
    public void IsAdjacentOrOverlapping_SamePosition_ReturnsTrue()
    {
        var state1 = CreateState();
        state1.Position = new Point(100, 100);
        var state2 = CreateState();
        state2.Position = new Point(100, 100);
        Assert.True(state1.IsAdjacentOrOverlapping(state2));
    }

    [Fact]
    public void IsWithinRect_Null_ReturnsFalse()
    {
        var state = CreateState();
        Assert.False(state.IsWithinRect(0, null));
    }

    // --- ReproductionWait countdown ---

    [Fact]
    public void AddTickToAge_DecrementsGrowthAndReproductionWait()
    {
        var state = CreateState();
        state.ResetReproductionWait();
        state.ResetGrowthWait();
        int reproWait = state.ReproductionWait;
        int growthWait = state.GrowthWait;
        state.AddTickToAge();
        Assert.Equal(reproWait - 1, state.ReproductionWait);
        Assert.Equal(growthWait - 1, state.GrowthWait);
    }

    // --- IsStopped ---

    [Fact]
    public void IsStopped_NoMoveAction_ReturnsTrue()
    {
        var state = CreateState();
        Assert.True(state.IsStopped);
    }
}
