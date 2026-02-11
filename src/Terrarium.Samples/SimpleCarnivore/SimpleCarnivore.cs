using System;
using System.Collections;
using System.Drawing;
using System.IO;
using OrganismBase;

// Assembly-level attributes identify this creature to the Terrarium engine
[assembly: OrganismClass("Terrarium.Samples.SimpleCarnivore.SimpleCarnivore")]
[assembly: AuthorInformation("Terrarium Team", "terrarium@example.com")]

namespace Terrarium.Samples.SimpleCarnivore;

/// <summary>
/// A basic carnivore that hunts other animals, kills them, then eats them.
/// Demonstrates: scanning, attacking, eating dead animals, movement, and targeting.
/// Strategy: High attack damage + speed. Finds prey, chases it down,
/// kills it, then feeds on the carcass.
/// </summary>
[Carnivore(true)]
[MatureSize(30)]
[AnimalSkin(AnimalSkinFamily.Scorpion)]
[MarkingColor(KnownColor.Red)]
// 100 points to distribute — focused on attack and pursuit
[MaximumEnergyPoints(0)]
[EatingSpeedPoints(0)]
[AttackDamagePoints(52)]
[DefendDamagePoints(0)]
[MaximumSpeedPoints(28)]
[CamouflagePoints(0)]
[EyesightPoints(20)]
public class SimpleCarnivore : Animal
{
    private AnimalState? targetAnimal;

    protected override void Initialize()
    {
        Load += LoadEvent;
        Idle += IdleEvent;
    }

    /// <summary>
    /// First event each turn — verify our target animal still exists.
    /// </summary>
    private void LoadEvent(object sender, LoadEventArgs e)
    {
        try
        {
            if (targetAnimal != null)
            {
                targetAnimal = (AnimalState?)LookFor(targetAnimal);
            }
        }
        catch (Exception exc)
        {
            WriteTrace(exc.ToString());
        }
    }

    /// <summary>
    /// Main logic: reproduce, find prey, attack or eat, or wait.
    /// </summary>
    private void IdleEvent(object sender, IdleEventArgs e)
    {
        try
        {
            if (CanReproduce)
                BeginReproduction(null);

            // If already busy, let the current action complete
            if (IsAttacking || IsMoving || IsEating)
                return;

            // Find a new target if we need one
            if (targetAnimal == null)
                FindNewTarget();

            if (targetAnimal != null)
            {
                if (targetAnimal.IsAlive)
                {
                    // Attack living prey
                    if (WithinAttackingRange(targetAnimal))
                    {
                        BeginAttacking(targetAnimal);
                    }
                    else
                    {
                        MoveToTarget();
                    }
                }
                else
                {
                    // Eat dead prey
                    if (WithinEatingRange(targetAnimal))
                    {
                        if (CanEat)
                            BeginEating(targetAnimal);
                    }
                    else
                    {
                        MoveToTarget();
                    }
                }
            }
            else
            {
                // No target found — conserve energy
                StopMoving();
            }
        }
        catch (Exception exc)
        {
            WriteTrace(exc.ToString());
        }
    }

    /// <summary>
    /// Scans for nearby animals of a different species to hunt.
    /// </summary>
    private void FindNewTarget()
    {
        try
        {
            ArrayList foundOrganisms = Scan();

            if (foundOrganisms.Count > 0)
            {
                foreach (OrganismState organismState in foundOrganisms)
                {
                    if (organismState is AnimalState animal && !IsMySpecies(organismState))
                    {
                        targetAnimal = animal;
                        return;
                    }
                }
            }
        }
        catch (Exception exc)
        {
            WriteTrace(exc.ToString());
        }
    }

    /// <summary>
    /// Moves toward the target animal at maximum speed.
    /// </summary>
    private void MoveToTarget()
    {
        try
        {
            if (targetAnimal == null)
                return;

            BeginMoving(new MovementVector(targetAnimal.Position, Species.MaximumSpeed));
        }
        catch (Exception exc)
        {
            WriteTrace(exc.ToString());
        }
    }

    public override void SerializeAnimal(MemoryStream m) { }
    public override void DeserializeAnimal(MemoryStream m) { }
}
