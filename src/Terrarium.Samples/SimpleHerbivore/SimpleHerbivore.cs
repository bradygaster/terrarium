using System;
using System.Collections;
using System.Drawing;
using System.IO;
using OrganismBase;

// Assembly-level attributes identify this creature to the Terrarium engine
[assembly: OrganismClass("Terrarium.Samples.SimpleHerbivore.SimpleHerbivore")]
[assembly: AuthorInformation("Terrarium Team", "terrarium@example.com")]

namespace Terrarium.Samples.SimpleHerbivore;

/// <summary>
/// A basic herbivore that searches for plants, eats them, and reproduces.
/// Demonstrates: movement, scanning, eating plants, and reproduction.
/// Strategy: High camouflage + eyesight. Finds plants, moves toward them,
/// eats, and reproduces as often as possible.
/// </summary>
[Carnivore(false)]
[MatureSize(26)]
[AnimalSkin(AnimalSkinFamily.Beetle)]
[MarkingColor(KnownColor.Green)]
// 100 points to distribute — focused on hiding and finding food
[MaximumEnergyPoints(0)]
[EatingSpeedPoints(0)]
[AttackDamagePoints(0)]
[DefendDamagePoints(0)]
[MaximumSpeedPoints(0)]
[CamouflagePoints(50)]
[EyesightPoints(50)]
public class SimpleHerbivore : Animal
{
    private PlantState? targetPlant;

    protected override void Initialize()
    {
        Load += LoadEvent;
        Idle += IdleEvent;
    }

    /// <summary>
    /// First event each turn — verify our target plant still exists.
    /// </summary>
    private void LoadEvent(object sender, LoadEventArgs e)
    {
        try
        {
            if (targetPlant != null)
            {
                targetPlant = (PlantState?)LookFor(targetPlant);
            }
        }
        catch (Exception exc)
        {
            WriteTrace(exc.ToString());
        }
    }

    /// <summary>
    /// Main logic: reproduce, find plants, eat, or wander randomly.
    /// </summary>
    private void IdleEvent(object sender, IdleEventArgs e)
    {
        try
        {
            if (CanReproduce)
                BeginReproduction(null);

            if (CanEat && !IsEating)
            {
                if (targetPlant != null)
                {
                    if (WithinEatingRange(targetPlant))
                    {
                        BeginEating(targetPlant);
                        if (IsMoving)
                            StopMoving();
                    }
                    else
                    {
                        if (!IsMoving)
                            BeginMoving(new MovementVector(targetPlant.Position, 2));
                    }
                }
                else
                {
                    if (!ScanForTargetPlant() && !IsMoving)
                    {
                        int randomX = OrganismRandom.Next(0, WorldWidth - 1);
                        int randomY = OrganismRandom.Next(0, WorldHeight - 1);
                        BeginMoving(new MovementVector(new Point(randomX, randomY), 2));
                    }
                }
            }
            else
            {
                if (IsMoving)
                    StopMoving();
            }
        }
        catch (Exception exc)
        {
            WriteTrace(exc.ToString());
        }
    }

    /// <summary>
    /// Scans for nearby plants and targets the first one found.
    /// </summary>
    private bool ScanForTargetPlant()
    {
        try
        {
            ArrayList foundOrganisms = Scan();

            if (foundOrganisms.Count > 0)
            {
                foreach (OrganismState organismState in foundOrganisms)
                {
                    if (organismState is PlantState plant)
                    {
                        targetPlant = plant;
                        BeginMoving(new MovementVector(organismState.Position, 2));
                        return true;
                    }
                }
            }
        }
        catch (Exception exc)
        {
            WriteTrace(exc.ToString());
        }

        return false;
    }

    public override void SerializeAnimal(MemoryStream m) { }
    public override void DeserializeAnimal(MemoryStream m) { }
}
