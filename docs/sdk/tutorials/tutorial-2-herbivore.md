# Tutorial 2: Creating a Herbivore

> **New to Terrarium?** Start with the [Getting Started Guide](../getting-started.md) for a 10-minute quickstart!

Herbivores are animals that eat plants. They must search for food, move around the world, eat, and reproduce. This tutorial builds a complete herbivore from scratch.

## Overview

Herbivores need to:
- Scan their surroundings to find plants
- Move toward food sources
- Eat plants when in range
- Reproduce when conditions are favorable
- Avoid starvation by managing energy

## Complete Herbivore Example

Let's build a functional herbivore step by step:

```csharp
using System;
using System.Collections;
using System.Drawing;
using System.IO;
using OrganismBase;

[assembly: OrganismClass("MyCreatures.GreenBeetle.GreenBeetle")]
[assembly: AuthorInformation("Your Name", "your.email@example.com")]

namespace MyCreatures.GreenBeetle;

/// <summary>
/// A camouflaged herbivore that searches for plants and eats them.
/// Strategy: High camouflage + eyesight to find food while staying hidden.
/// </summary>
[Carnivore(false)]                    // Herbivore
[MatureSize(26)]
[AnimalSkin(AnimalSkinFamily.Beetle)]
[MarkingColor(KnownColor.Green)]
// 100 points to distribute across characteristics
[MaximumEnergyPoints(0)]
[EatingSpeedPoints(0)]
[AttackDamagePoints(0)]
[DefendDamagePoints(0)]
[MaximumSpeedPoints(0)]
[CamouflagePoints(50)]                // Stealth
[EyesightPoints(50)]                  // Vision
public class GreenBeetle : Animal
{
    private PlantState? targetPlant;

    protected override void Initialize()
    {
        Load += LoadEvent;
        Idle += IdleEvent;
    }

    private void LoadEvent(object sender, LoadEventArgs e)
    {
        try
        {
            // Verify target still exists
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

    private void IdleEvent(object sender, IdleEventArgs e)
    {
        try
        {
            // Priority 1: Reproduce when possible
            if (CanReproduce)
                BeginReproduction(null);

            // Priority 2: Eat when hungry
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
                    // No target - scan or wander
                    if (!ScanForTargetPlant() && !IsMoving)
                    {
                        WanderRandomly();
                    }
                }
            }
            else
            {
                // Full energy - stop moving
                if (IsMoving)
                    StopMoving();
            }
        }
        catch (Exception exc)
        {
            WriteTrace(exc.ToString());
        }
    }

    private bool ScanForTargetPlant()
    {
        try
        {
            ArrayList foundOrganisms = Scan();

            foreach (OrganismState organismState in foundOrganisms)
            {
                if (organismState is PlantState plant)
                {
                    targetPlant = plant;
                    BeginMoving(new MovementVector(plant.Position, 2));
                    return true;
                }
            }
        }
        catch (Exception exc)
        {
            WriteTrace(exc.ToString());
        }

        return false;
    }

    private void WanderRandomly()
    {
        int randomX = OrganismRandom.Next(0, WorldWidth - 1);
        int randomY = OrganismRandom.Next(0, WorldHeight - 1);
        BeginMoving(new MovementVector(new Point(randomX, randomY), 2));
    }

    public override void SerializeAnimal(MemoryStream m) { }
    public override void DeserializeAnimal(MemoryStream m) { }
}
```

## Understanding Animal Attributes

### The Carnivore Attribute

```csharp
[Carnivore(false)]  // Herbivore - eats plants
[Carnivore(true)]   // Carnivore - eats animals (Tutorial 3)
```

This is the **most important attribute** for animals. It determines:
- What your animal can eat
- Its lifespan multiplier
- Its attack/defense capabilities

### Size and Appearance

```csharp
[MatureSize(26)]                      // Size when fully grown (25-48)
[AnimalSkin(AnimalSkinFamily.Beetle)] // Visual appearance
[MarkingColor(KnownColor.Green)]      // Creature color
```

Available animal skins:
- `AnimalSkinFamily.Beetle`
- `AnimalSkinFamily.Scorpion`
- And others (see API reference)

### Characteristic Points

**Every animal has exactly 100 points to distribute** across seven characteristics:

```csharp
[MaximumEnergyPoints(0)]    // Energy storage capacity
[EatingSpeedPoints(0)]      // How fast you eat
[AttackDamagePoints(0)]     // Damage dealt when attacking
[DefendDamagePoints(0)]     // Damage resistance
[MaximumSpeedPoints(0)]     // Movement speed
[CamouflagePoints(50)]      // Hiding from predators
[EyesightPoints(50)]        // Detection range
```

**The total MUST equal 100**. The engine validates this.

#### Characteristic Strategy for Herbivores

**Stealth Herbivore** (like our example):
```csharp
[CamouflagePoints(50)]   // Hide from predators
[EyesightPoints(50)]     // Find food from distance
```

**Fast Grazer**:
```csharp
[MaximumSpeedPoints(40)] // Outrun predators
[EyesightPoints(40)]     // Find food quickly
[MaximumEnergyPoints(20)] // Sustain longer chases
```

**Efficient Eater**:
```csharp
[EatingSpeedPoints(50)]  // Eat quickly and move on
[MaximumSpeedPoints(30)] // Decent speed
[EyesightPoints(20)]     // Basic vision
```

## Animal Event Model

Animals respond to events that occur each turn. The two most important events:

### Load Event

Fires **first** each turn, before any other events:

```csharp
Load += LoadEvent;

private void LoadEvent(object sender, LoadEventArgs e)
{
    // Verify your targets still exist
    // Update tracking information
    // Refresh state
}
```

Use `Load` to:
- Validate that tracked organisms still exist
- Refresh organism states
- Clean up dead references

### Idle Event

Fires when your animal has **no actions in progress**:

```csharp
Idle += IdleEvent;

private void IdleEvent(object sender, IdleEventArgs e)
{
    // Decide what to do next
    // Start new actions
    // Implement your strategy
}
```

Use `Idle` to:
- Check if you can reproduce
- Decide whether to eat, move, or rest
- Implement your survival strategy

### Other Events

```csharp
MoveCompleted += OnMoveCompleted;
EatCompleted += OnEatCompleted;
ReproduceCompleted += OnReproduceCompleted;
Attacked += OnAttacked;
```

These fire when specific actions complete (covered in advanced tutorials).

## Core Animal Capabilities

### Scanning for Organisms

```csharp
ArrayList foundOrganisms = Scan();
```

Returns all organisms within your eyesight range. Eyesight is determined by:
- Base eyesight (5 unit radius)
- `EyesightPoints` attribute
- Organism's camouflage

```csharp
foreach (OrganismState organism in foundOrganisms)
{
    if (organism is PlantState plant)
    {
        // Found a plant!
    }
    else if (organism is AnimalState animal)
    {
        // Found an animal!
    }
}
```

### Looking for Specific Organisms

```csharp
OrganismState? current = LookFor(previousState);
```

Updates the state of a specific organism you're tracking. Returns `null` if:
- The organism died
- The organism moved out of range
- The organism's camouflage hides it from you

### Moving

```csharp
BeginMoving(new MovementVector(destination, speed));
```

Start moving toward a destination at a given speed:

```csharp
// Move to a plant's position at speed 2
BeginMoving(new MovementVector(targetPlant.Position, 2));

// Move to a random point at maximum speed
Point randomPoint = new Point(
    OrganismRandom.Next(0, WorldWidth - 1),
    OrganismRandom.Next(0, WorldHeight - 1)
);
BeginMoving(new MovementVector(randomPoint, Species.MaximumSpeed));
```

**Speed constraints**:
- Speed is determined by `MaximumSpeedPoints`
- Cannot exceed `Species.MaximumSpeed`
- Higher speed costs more energy
- Must be within world bounds

```csharp
StopMoving();  // Cancel current movement
```

### Eating

```csharp
if (CanEat && WithinEatingRange(targetPlant))
{
    BeginEating(targetPlant);
}
```

`CanEat` is true when:
- Your energy state is Normal or below
- You're not currently eating

Herbivores can only eat:
- Plants (`PlantState`)
- Plants that exist and are visible

```csharp
bool inRange = WithinEatingRange(organism);
```

Eating range means the organisms are adjacent or overlapping.

### Reproduction

```csharp
if (CanReproduce)
{
    BeginReproduction(null);  // null = no DNA modification
}
```

`CanReproduce` is true when:
- You're mature (fully grown)
- Your energy state is Normal or better
- You're not already reproducing
- The reproduction cooldown has passed

Reproduction:
- Creates an offspring of your species
- Costs significant energy
- Has a cooldown period
- Offspring inherits your characteristics

## Important Properties

### State Information

```csharp
Point position = Position;              // Current location
int age = State.TickAge;                // Age in ticks
bool mature = State.IsMature;           // Fully grown?
EnergyState energy = State.EnergyState; // Energy level
int generation = State.Generation;      // Generation number
```

### World Information

```csharp
int width = WorldWidth;                 // World width in pixels
int height = WorldHeight;               // World height in pixels
Random rng = OrganismRandom;            // Random number generator
```

### Action Status

```csharp
bool moving = IsMoving;                 // Currently moving?
bool eating = IsEating;                 // Currently eating?
bool reproducing = IsReproducing;       // Currently reproducing?
```

## Herbivore Strategy Patterns

### Pattern 1: Aggressive Eater

```csharp
// Always prioritize eating over everything
private void IdleEvent(object sender, IdleEventArgs e)
{
    if (CanEat && targetPlant != null)
    {
        if (WithinEatingRange(targetPlant))
            BeginEating(targetPlant);
        else
            BeginMoving(new MovementVector(targetPlant.Position, Species.MaximumSpeed));
    }
    else if (CanReproduce)
    {
        BeginReproduction(null);
    }
    else
    {
        ScanForTargetPlant();
    }
}
```

### Pattern 2: Opportunistic Reproducer

```csharp
// Reproduce whenever possible, eat when necessary
private void IdleEvent(object sender, IdleEventArgs e)
{
    if (CanReproduce)
    {
        BeginReproduction(null);
        return;
    }

    if (State.EnergyState <= EnergyState.Hungry)
    {
        FindFoodAndEat();
    }
}
```

### Pattern 3: Territorial

```csharp
private Point homePosition;
private const int TerritoryRadius = 200;

private void IdleEvent(object sender, IdleEventArgs e)
{
    // Stay within territory
    if (Vector.Subtract(Position, homePosition).Magnitude > TerritoryRadius)
    {
        BeginMoving(new MovementVector(homePosition, 2));
        return;
    }

    // Normal eating behavior within territory
    FindFoodAndEat();
}
```

## Common Mistakes

### Forgetting to Initialize Events

```csharp
// ❌ Wrong - Initialize() not overridden
public class BadHerbivore : Animal
{
    private void IdleEvent(object sender, IdleEventArgs e)
    {
        // This will NEVER fire!
    }
}

// ✅ Correct
public class GoodHerbivore : Animal
{
    protected override void Initialize()
    {
        Idle += IdleEvent;
    }

    private void IdleEvent(object sender, IdleEventArgs e)
    {
        // This fires every idle turn
    }
}
```

### Not Using Try-Catch

```csharp
// ❌ Wrong - exceptions crash your creature
private void IdleEvent(object sender, IdleEventArgs e)
{
    BeginEating(targetPlant);  // Might throw exception!
}

// ✅ Correct
private void IdleEvent(object sender, IdleEventArgs e)
{
    try
    {
        if (targetPlant != null && WithinEatingRange(targetPlant))
        {
            BeginEating(targetPlant);
        }
    }
    catch (Exception exc)
    {
        WriteTrace(exc.ToString());
    }
}
```

### Moving When Already Moving

```csharp
// ❌ Wrong - tries to move every frame
private void IdleEvent(object sender, IdleEventArgs e)
{
    BeginMoving(new MovementVector(target.Position, 2));
}

// ✅ Correct - check if already moving
private void IdleEvent(object sender, IdleEventArgs e)
{
    if (!IsMoving)
    {
        BeginMoving(new MovementVector(target.Position, 2));
    }
}
```

### Characteristic Points Don't Sum to 100

```csharp
// ❌ Wrong - sums to 110
[MaximumEnergyPoints(10)]
[EyesightPoints(50)]
[CamouflagePoints(50)]
// Missing attributes still count as 0

// ✅ Correct - sums to 100
[MaximumEnergyPoints(0)]
[EatingSpeedPoints(0)]
[AttackDamagePoints(0)]
[DefendDamagePoints(0)]
[MaximumSpeedPoints(0)]
[CamouflagePoints(50)]
[EyesightPoints(50)]
// Total: 100
```

## Testing Your Herbivore

1. Build your project
2. Introduce several plants first to create food sources
3. Introduce your herbivore
4. Watch it scan, move, eat, and reproduce

## Next Steps

Ready to create a predator?

- [Tutorial 3: Creating a Carnivore](./tutorial-3-carnivore.md)

## Reference

- [Animal Class API Documentation](../api/animal.md)
- [Animal Attributes Reference](../api/attributes.md#animal-attributes)
- [Event Reference](../api/events.md)
- [State Classes Reference](../api/state.md)
