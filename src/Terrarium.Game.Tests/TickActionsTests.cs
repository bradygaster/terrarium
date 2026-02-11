using System.Drawing;
using System.Reflection;
using OrganismBase;
using Xunit;

namespace Terrarium.Game.Tests;

/// <summary>
/// Tests for TickActions: action aggregation for move, attack, eat, reproduce, defend.
/// </summary>
public class TickActionsTests
{
    private static readonly IAnimalSpecies AnimalSpecies = new MockAnimalSpecies();
    private static readonly IPlantSpecies PlantSpecies = new MockPlantSpecies();

    private static T CreateAction<T>(params object?[] args)
    {
        var ctor = typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotEmpty(ctor);
        return (T)ctor[0].Invoke(args)!;
    }

    // --- Empty state ---

    [Fact]
    public void TickActions_StartsEmpty()
    {
        var actions = new TickActions();

        Assert.Empty(actions.MoveToActions);
        Assert.Empty(actions.AttackActions);
        Assert.Empty(actions.EatActions);
        Assert.Empty(actions.ReproduceActions);
        Assert.Empty(actions.DefendActions);
    }

    // --- ReadOnly dictionaries ---

    [Fact]
    public void MoveToActions_IsReadOnly()
    {
        var actions = new TickActions();
        var dict = actions.MoveToActions;
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, MoveToAction>>(dict);
    }

    [Fact]
    public void AttackActions_IsReadOnly()
    {
        var actions = new TickActions();
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, AttackAction>>(actions.AttackActions);
    }

    [Fact]
    public void EatActions_IsReadOnly()
    {
        var actions = new TickActions();
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, EatAction>>(actions.EatActions);
    }

    [Fact]
    public void ReproduceActions_IsReadOnly()
    {
        var actions = new TickActions();
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, ReproduceAction>>(actions.ReproduceActions);
    }

    [Fact]
    public void DefendActions_IsReadOnly()
    {
        var actions = new TickActions();
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, DefendAction>>(actions.DefendActions);
    }

    // --- Action types have expected properties ---

    [Fact]
    public void MoveToAction_HasExpectedProperties()
    {
        var vector = new MovementVector(new Point(100, 200), 5);
        var action = CreateAction<MoveToAction>("org-1", 1, vector);

        Assert.Equal("org-1", action.OrganismID);
        Assert.Equal(1, action.ActionID);
        Assert.Equal(vector.Destination, action.MovementVector.Destination);
        Assert.Equal(vector.Speed, action.MovementVector.Speed);
    }

    [Fact]
    public void AttackAction_HasTargetAnimal()
    {
        var target = new AnimalState("target-1", AnimalSpecies, 0, EnergyState.Full, 10);
        var action = CreateAction<AttackAction>("attacker-1", 2, target);

        Assert.Same(target, action.TargetAnimal);
    }

    [Fact]
    public void EatAction_HasTargetOrganism()
    {
        var target = new PlantState("food-1", PlantSpecies, 0, EnergyState.Full, 10);
        var action = CreateAction<EatAction>("eater-1", 3, (OrganismState)target);

        Assert.Same(target, action.TargetOrganism);
    }

    [Fact]
    public void DefendAction_HasTargetAnimal()
    {
        var target = new AnimalState("att-1", AnimalSpecies, 0, EnergyState.Full, 10);
        var action = CreateAction<DefendAction>("def-1", 7, target);

        Assert.Same(target, action.TargetAnimal);
    }

    [Fact]
    public void ReproduceAction_WithDna_ReturnsClone()
    {
        var dna = new byte[] { 1, 2, 3 };
        var action = CreateAction<ReproduceAction>("p-1", 4, dna);

        Assert.NotNull(action.Dna);
        Assert.Equal(dna, action.Dna);
        Assert.NotSame(dna, action.Dna);
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
