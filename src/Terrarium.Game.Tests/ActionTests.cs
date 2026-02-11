using System;
using System.Drawing;
using System.Reflection;
using OrganismBase;
using Xunit;

namespace Terrarium.Game.Tests;

/// <summary>
/// Tests that MoveToAction, AttackAction, EatAction, and ReproduceAction
/// can be created with proper parameters via reflection (constructors are internal)
/// and that PendingActions properly holds them.
/// </summary>
public class ActionTests
{
    private static readonly IAnimalSpecies AnimalSpecies = new MockAnimalSpecies();
    private static readonly IPlantSpecies PlantSpecies = new MockPlantSpecies();

    private static AnimalState CreateAnimalState(string id = "animal-1")
        => new AnimalState(id, AnimalSpecies, 0, EnergyState.Full, 10);

    private static PlantState CreatePlantState(string id = "plant-1")
        => new PlantState(id, PlantSpecies, 0, EnergyState.Full, 10);

    // Helper to construct internal action types via reflection
    private static T CreateAction<T>(params object?[] args)
    {
        var type = typeof(T);
        var ctor = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotEmpty(ctor);
        var instance = ctor[0].Invoke(args);
        Assert.NotNull(instance);
        return (T)instance!;
    }

    // --- MoveToAction ---

    [Fact]
    public void MoveToAction_CanBeCreated_WithMovementVector()
    {
        var vector = new MovementVector(new Point(100, 200), 5);
        var action = CreateAction<MoveToAction>("org-1", 1, vector);

        Assert.Equal("org-1", action.OrganismID);
        Assert.Equal(1, action.ActionID);
        Assert.Equal(vector, action.MovementVector);
    }

    [Fact]
    public void MoveToAction_ToString_ContainsVectorInfo()
    {
        var vector = new MovementVector(new Point(50, 75), 3);
        var action = CreateAction<MoveToAction>("org-1", 1, vector);
        var str = action.ToString();
        Assert.Contains("50", str);
        Assert.Contains("75", str);
    }

    // --- AttackAction ---

    [Fact]
    public void AttackAction_CanBeCreated_WithTargetAnimal()
    {
        var target = CreateAnimalState("target-1");
        var action = CreateAction<AttackAction>("attacker-1", 2, target);

        Assert.Equal("attacker-1", action.OrganismID);
        Assert.Equal(2, action.ActionID);
        Assert.Same(target, action.TargetAnimal);
    }

    [Fact]
    public void AttackAction_ToString_ContainsTargetId()
    {
        var target = CreateAnimalState("target-2");
        var action = CreateAction<AttackAction>("attacker-1", 2, target);
        Assert.Contains("target-2", action.ToString());
    }

    // --- EatAction ---

    [Fact]
    public void EatAction_CanBeCreated_WithTargetOrganism()
    {
        var target = CreatePlantState("food-1");
        var action = CreateAction<EatAction>("eater-1", 3, (OrganismState)target);

        Assert.Equal("eater-1", action.OrganismID);
        Assert.Equal(3, action.ActionID);
        Assert.Same(target, action.TargetOrganism);
    }

    [Fact]
    public void EatAction_CanTargetAnimal()
    {
        var target = CreateAnimalState("prey-1");
        var action = CreateAction<EatAction>("eater-1", 3, (OrganismState)target);
        Assert.Same(target, action.TargetOrganism);
    }

    [Fact]
    public void EatAction_ToString_ContainsTargetId()
    {
        var target = CreatePlantState("food-2");
        var action = CreateAction<EatAction>("eater-1", 3, (OrganismState)target);
        Assert.Contains("food-2", action.ToString());
    }

    // --- ReproduceAction ---

    [Fact]
    public void ReproduceAction_CanBeCreated_WithDna()
    {
        var dna = new byte[] { 1, 2, 3, 4 };
        var action = CreateAction<ReproduceAction>("parent-1", 4, dna);

        Assert.Equal("parent-1", action.OrganismID);
        Assert.Equal(4, action.ActionID);
        Assert.NotNull(action.Dna);
        Assert.Equal(dna, action.Dna);
    }

    [Fact]
    public void ReproduceAction_CanBeCreated_WithNullDna()
    {
        var action = CreateAction<ReproduceAction>("parent-1", 5, (byte[]?)null);
        Assert.Null(action.Dna);
    }

    [Fact]
    public void ReproduceAction_Dna_ReturnsClone()
    {
        var dna = new byte[] { 10, 20, 30 };
        var action = CreateAction<ReproduceAction>("parent-1", 6, dna);
        var returned = action.Dna;
        Assert.NotSame(dna, returned);
        Assert.Equal(dna, returned);
    }

    // --- DefendAction ---

    [Fact]
    public void DefendAction_CanBeCreated_WithTargetAnimal()
    {
        var target = CreateAnimalState("attacker-1");
        var action = CreateAction<DefendAction>("defender-1", 7, target);

        Assert.Equal("defender-1", action.OrganismID);
        Assert.Equal(7, action.ActionID);
        Assert.Same(target, action.TargetAnimal);
    }

    // --- PendingActions integration ---

    [Fact]
    public void PendingActions_CanHoldMoveToAction()
    {
        var actions = new PendingActions();
        var vector = new MovementVector(new Point(50, 50), 5);
        var moveAction = CreateAction<MoveToAction>("org-1", 1, vector);
        actions.SetMoveToAction(moveAction);
        Assert.Same(moveAction, actions.MoveToAction);
    }

    [Fact]
    public void PendingActions_CanHoldAttackAction()
    {
        var actions = new PendingActions();
        var target = CreateAnimalState("target-1");
        var attackAction = CreateAction<AttackAction>("org-1", 1, target);
        actions.SetAttackAction(attackAction);
        Assert.Same(attackAction, actions.AttackAction);
    }

    [Fact]
    public void PendingActions_CanHoldEatAction()
    {
        var actions = new PendingActions();
        var target = CreatePlantState("food-1");
        var eatAction = CreateAction<EatAction>("org-1", 1, (OrganismState)target);
        actions.SetEatAction(eatAction);
        Assert.Same(eatAction, actions.EatAction);
    }

    [Fact]
    public void PendingActions_CanHoldReproduceAction()
    {
        var actions = new PendingActions();
        var reproduceAction = CreateAction<ReproduceAction>("org-1", 1, (byte[]?)null);
        actions.SetReproduceAction(reproduceAction);
        Assert.Same(reproduceAction, actions.ReproduceAction);
    }

    [Fact]
    public void PendingActions_CanClearActions()
    {
        var actions = new PendingActions();
        var vector = new MovementVector(new Point(50, 50), 5);
        actions.SetMoveToAction(CreateAction<MoveToAction>("org-1", 1, vector));
        actions.SetMoveToAction(null);
        Assert.Null(actions.MoveToAction);
    }

    [Fact]
    public void PendingActions_Immutable_ThrowsOnSet()
    {
        var actions = new PendingActions();
        actions.MakeImmutable();
        Assert.Throws<ApplicationException>(() =>
            actions.SetMoveToAction(CreateAction<MoveToAction>("org-1", 1,
                new MovementVector(new Point(50, 50), 5))));
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
