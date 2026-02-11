using OrganismBase;
using Xunit;

namespace Terrarium.OrganismBase.Tests;

public class GrowthTests
{
    [Fact]
    public void AnimalState_Grow_WhenEligible_ReturnsGrownState()
    {
        var species = new MockAnimalSpecies { MatureRadius = 20, GrowthWait = 0 };
        var state = new AnimalState("grow1", species, 0, EnergyState.Full, 5);
        var grown = state.Grow();
        Assert.NotSame(state, grown);
        Assert.Equal(6, grown.Radius);
    }

    [Fact]
    public void AnimalState_Grow_AlreadyMature_ReturnsSelf()
    {
        var species = new MockAnimalSpecies { MatureRadius = 5 };
        var state = new AnimalState("grow2", species, 0, EnergyState.Full, 5);
        var result = state.Grow();
        Assert.Same(state, result);
    }

    [Fact]
    public void AnimalState_Grow_LowEnergy_ReturnsSelf()
    {
        var species = new MockAnimalSpecies { MatureRadius = 20, GrowthWait = 0 };
        var state = new AnimalState("grow3", species, 0, EnergyState.Hungry, 5);
        var result = state.Grow();
        Assert.Same(state, result);
    }

    [Fact]
    public void AnimalState_Grow_OnImmutable_Throws()
    {
        var species = new MockAnimalSpecies { MatureRadius = 20, GrowthWait = 0 };
        var state = new AnimalState("grow4", species, 0, EnergyState.Full, 5);
        state.MakeImmutable();
        Assert.Throws<GameEngineException>(() => state.Grow());
    }

    [Fact]
    public void PlantState_Grow_WhenEligible_ReturnsGrownState()
    {
        var species = new MockPlantSpecies { MatureRadius = 20, GrowthWait = 0 };
        var state = new PlantState("pgrow1", species, 0, EnergyState.Full, 5);
        var grown = state.Grow();
        Assert.NotSame(state, grown);
        Assert.Equal(6, grown.Radius);
    }

    [Fact]
    public void PlantState_Grow_AlreadyMature_ReturnsSelf()
    {
        var species = new MockPlantSpecies { MatureRadius = 5 };
        var state = new PlantState("pgrow2", species, 0, EnergyState.Full, 5);
        var result = state.Grow();
        Assert.Same(state, result);
    }

    [Fact]
    public void PlantState_Grow_OnImmutable_Throws()
    {
        var species = new MockPlantSpecies { MatureRadius = 20, GrowthWait = 0 };
        var state = new PlantState("pgrow3", species, 0, EnergyState.Full, 5);
        state.MakeImmutable();
        Assert.Throws<GameEngineException>(() => state.Grow());
    }
}
