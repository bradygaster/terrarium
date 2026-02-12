using System;
using System.Collections;
using System.Drawing;
using System.IO;
using OrganismBase;

// Assembly-level attributes identify this creature to the Terrarium engine
[assembly: OrganismClass("TerrariumCreature.TerrariumCreature")]
[assembly: AuthorInformation("AUTHOR_NAME", "AUTHOR_EMAIL")]

namespace TerrariumCreature;

#if (IsAnimal)
/// <summary>
/// TODO: Describe your creature's strategy and behavior here.
/// </summary>
[Carnivore(CARNIVORE_VALUE)]
[MatureSize(26)]
[AnimalSkin(AnimalSkinFamily.Beetle)]
[MarkingColor(KnownColor.Green)]
// Distribute 100 points across these characteristics
[MaximumEnergyPoints(0)]
[EatingSpeedPoints(0)]
[AttackDamagePoints(0)]
[DefendDamagePoints(0)]
[MaximumSpeedPoints(25)]
[CamouflagePoints(25)]
[EyesightPoints(50)]
public class TerrariumCreature : Animal
{
    protected override void Initialize()
    {
        // Wire up event handlers
        Load += OnLoad;
        Idle += OnIdle;
        Attacked += OnAttacked;
        AttackedAnimal += OnAttackedAnimal;
    }

    /// <summary>
    /// Called at the start of each turn. Use to verify state.
    /// </summary>
    private void OnLoad(object sender, LoadEventArgs e)
    {
        try
        {
            // TODO: Update internal state, verify targets still exist
        }
        catch (Exception ex)
        {
            WriteTrace($"Load error: {ex.Message}");
        }
    }

    /// <summary>
    /// Main behavior logic — called when the creature is idle.
    /// </summary>
    private void OnIdle(object sender, IdleEventArgs e)
    {
        try
        {
            // Reproduce when possible
            if (CanReproduce)
            {
                BeginReproduction(null);
            }

            // TODO: Implement your creature's behavior
            // Examples:
            // - Scan for food/enemies: ArrayList organisms = Scan();
            // - Move to a location: BeginMoving(new MovementVector(targetPosition, speed));
            // - Attack: BeginAttacking(targetAnimal);
            // - Eat: BeginEating(targetOrganism);
            // - Defend: BeginDefending();

            // Simple example: wander randomly
            if (!IsMoving)
            {
                int randomX = OrganismRandom.Next(0, WorldWidth - 1);
                int randomY = OrganismRandom.Next(0, WorldHeight - 1);
                BeginMoving(new MovementVector(new Point(randomX, randomY), 2));
            }
        }
        catch (Exception ex)
        {
            WriteTrace($"Idle error: {ex.Message}");
        }
    }

    /// <summary>
    /// Called when this creature is attacked by another animal.
    /// </summary>
    private void OnAttacked(object sender, AttackedEventArgs e)
    {
        try
        {
            // TODO: React to being attacked
            // Options: flee, defend, or counter-attack
        }
        catch (Exception ex)
        {
            WriteTrace($"Attacked error: {ex.Message}");
        }
    }

    /// <summary>
    /// Called when this creature successfully attacks another animal.
    /// </summary>
    private void OnAttackedAnimal(object sender, AttackedEventArgs e)
    {
        try
        {
            // TODO: React to successful attack
        }
        catch (Exception ex)
        {
            WriteTrace($"AttackedAnimal error: {ex.Message}");
        }
    }

    public override void SerializeAnimal(MemoryStream m)
    {
        // TODO: Serialize any state that needs to persist across turns
    }

    public override void DeserializeAnimal(MemoryStream m)
    {
        // TODO: Deserialize state
    }
}
#endif
#if (IsPlant)
/// <summary>
/// TODO: Describe your plant's strategy here.
/// </summary>
[MatureSize(24)]
[PlantSkin(PlantSkinFamily.Plant)]
[SeedSpreadDistance(0)]
[MarkingColor(KnownColor.Green)]
public class TerrariumCreature : Plant
{
    /// <summary>
    /// Plants grow and reproduce automatically.
    /// Override Initialize() if you need custom event handlers.
    /// </summary>

    public override void SerializePlant(MemoryStream m)
    {
        // TODO: Serialize any state that needs to persist across turns
    }

    public override void DeserializePlant(MemoryStream m)
    {
        // TODO: Deserialize state
    }
}
#endif
