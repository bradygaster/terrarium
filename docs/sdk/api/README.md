# OrganismBase API Reference

Complete API documentation for developing Terrarium creatures using the OrganismBase library.

> **New to Terrarium?** Start with the [Getting Started Guide](../getting-started.md) for a hands-on introduction!

## Overview

The OrganismBase API provides the foundation for creating autonomous organisms in the Terrarium ecosystem. All organisms derive from base classes in the `OrganismBase` namespace.

## Core Namespace

```csharp
using OrganismBase;
```

**Important**: The namespace is `OrganismBase`, not `Terrarium.OrganismBase`.

## Base Classes

### Organism

Abstract base class for all life forms. Provides:
- Reproduction capabilities
- Energy management
- Position and state tracking
- Random number generation

[→ Organism API Documentation](./organism.md)

### Animal

Base class for mobile creatures (herbivores and carnivores). Provides:
- Movement capabilities
- Scanning for other organisms
- Eating (plants or animals)
- Attacking and defending
- Event-driven behavior model

[→ Animal API Documentation](./animal.md)

### Plant

Base class for stationary organisms. Provides:
- Automatic growth and reproduction
- Light-to-energy conversion
- Simple lifecycle management

[→ Plant API Documentation](./plant.md)

## State Classes

Immutable state snapshots representing organisms at a point in time:

- **OrganismState** - Base state for all organisms
- **AnimalState** - State for animals (includes damage, movement, antennas)
- **PlantState** - State for plants (includes height, food chunks)

[→ State Classes Documentation](./state.md)

## Attributes

Attributes define organism characteristics at compile-time:

### Assembly Attributes

Required for all organisms:

- `[assembly: OrganismClass]` - Identifies the creature to the engine
- `[assembly: AuthorInformation]` - Credits the creator

### Organism Attributes

Appearance and size:

- `[MatureSize]` - Size when fully grown (25-48)
- `[MarkingColor]` - Visual color
- `[AnimalSkin]` / `[PlantSkin]` - Visual appearance family

### Animal-Specific Attributes

Behavior:

- `[Carnivore]` - Herbivore (false) or carnivore (true)

Characteristics (100 points total):

- `[MaximumEnergyPoints]` - Energy storage capacity
- `[EatingSpeedPoints]` - Eating efficiency
- `[AttackDamagePoints]` - Attack damage
- `[DefendDamagePoints]` - Damage resistance  
- `[MaximumSpeedPoints]` - Movement speed
- `[CamouflagePoints]` - Hiding ability
- `[EyesightPoints]` - Detection range

### Plant-Specific Attributes

- `[SeedSpreadDistance]` - Reproduction range (0-1000)

[→ Complete Attributes Reference](./attributes.md)

## Events

Animal behavior is driven by events:

- `Load` - Fires first each turn
- `Idle` - Fires when no actions are in progress
- `MoveCompleted` - Movement finished
- `EatCompleted` - Eating finished
- `AttackCompleted` - Attack finished
- `DefendCompleted` - Defense finished
- `ReproduceCompleted` - Reproduction finished
- `Attacked` - Another animal attacked you
- `Born` - Creature just born
- `Teleported` - Moved by the engine

[→ Events Documentation](./events.md)

## Actions

Actions represent ongoing behaviors:

- **MoveToAction** - Moving to a destination
- **EatAction** - Eating an organism
- **AttackAction** - Attacking an animal
- **DefendAction** - Defending against an attack
- **ReproduceAction** - Reproducing

[→ Actions Documentation](./actions.md)

## Enumerations

### EnergyState

Organism energy levels:

- `Full` - Maximum energy
- `Normal` - Healthy energy
- `Hungry` - Low energy
- `Deterioration` - Critical energy (organism is dying)

### DisplayAction

Visual representation of organism behavior:

- `NoAction` - Idle
- `Moved` - Movement
- `Attacked` - Attack action
- `Defended` - Defense action
- `Ate` - Eating
- `Reproduced` - Reproduction
- `Teleported` - Moved by engine
- `Died` - Just died
- `Dead` - Corpse

### AnimalSkinFamily

Available animal appearances:

- `Beetle`
- `Scorpion`
- And others (see full documentation)

### PlantSkinFamily

Available plant appearances:

- `Plant` - Standard plant

### PopulationChangeReason

Why an organism died:

- `NotDead` - Still alive
- `Killed` - Killed by another organism
- `Starved` - Ran out of energy
- `OldAge` - Natural death
- `Sick` - Disease
- `Error` - Exception occurred
- `Timeout` - Took too long to process

[→ Enumerations Documentation](./enumerations.md)

## Engine Settings

Constants defining world physics and biology:

```csharp
OrganismBase.EngineSettings
```

Key constants:

- `MaxMatureSize` = 48
- `MinMatureSize` = 25
- `MaxAvailableCharacteristicPoints` = 100
- `ViewPortWidth` = 800
- `ViewPortHeight` = 450
- `TicksToIncubate` = 10
- `TimeToRot` = 60
- And many more...

[→ EngineSettings Documentation](./engine-settings.md)

## Interfaces

Internal interfaces used by the engine to communicate with organisms:

- `ISpecies` - Species characteristics
- `IAnimalSpecies` - Animal-specific species info
- `IOrganismWorldBoundary` - World interaction boundary
- `IAnimalWorldBoundary` - Animal-specific boundary
- `IPlantWorldBoundary` - Plant-specific boundary

Generally, you won't implement these directly. The engine provides implementations.

[→ Interfaces Documentation](./interfaces.md)

## Exceptions

Exceptions thrown by the API:

- `AlreadyFullException` - Cannot eat when energy is full
- `AlreadyReproducingException` - Already reproducing
- `ImproperFoodException` - Wrong food type for this organism
- `NotDeadException` - Cannot eat living animal (carnivores only)
- `NotEnoughEnergyException` - Insufficient energy for action
- `NotHungryException` - Cannot attack when well-fed (herbivores)
- `NotMatureException` - Must be fully grown
- `NotReadyToReproduceException` - Reproduction on cooldown
- `NotVisibleException` - Target organism not visible
- `NotWithinDistanceException` - Target out of range
- `OutOfBoundsException` - Destination outside world
- `TooFastException` - Speed exceeds maximum
- `GameEngineException` - General engine error

[→ Exceptions Documentation](./exceptions.md)

## Helper Classes

### MovementVector

Defines movement direction and speed:

```csharp
var vector = new MovementVector(destinationPoint, speed);
```

### Vector

Math utilities for position calculations:

```csharp
Vector direction = Vector.Subtract(pointA, pointB);
double distance = direction.Magnitude;
```

### AntennaState

Communication between organisms (advanced topic):

```csharp
animal.Antennas = new AntennaState(data);
```

[→ Helper Classes Documentation](./helpers.md)

## Quick Reference

### Creating an Organism

1. Create a class deriving from `Animal` or `Plant`
2. Add assembly attributes: `[OrganismClass]` and `[AuthorInformation]`
3. Add organism attributes (size, appearance, characteristics)
4. Override `Initialize()` to register event handlers
5. Implement event handlers with your behavior logic
6. Override serialization methods (can be empty for stateless organisms)

### Typical Animal Structure

```csharp
using System;
using System.Collections;
using System.IO;
using OrganismBase;

[assembly: OrganismClass("MyNamespace.MyAnimal")]
[assembly: AuthorInformation("Name", "email")]

namespace MyNamespace;

[Carnivore(false)]
[MatureSize(26)]
[AnimalSkin(AnimalSkinFamily.Beetle)]
[MarkingColor(KnownColor.Green)]
// ... characteristic points (must sum to 100)
public class MyAnimal : Animal
{
    protected override void Initialize()
    {
        Load += LoadEvent;
        Idle += IdleEvent;
    }

    private void LoadEvent(object sender, LoadEventArgs e)
    {
        // Validate state each turn
    }

    private void IdleEvent(object sender, IdleEventArgs e)
    {
        // Decision logic here
    }

    public override void SerializeAnimal(MemoryStream m) { }
    public override void DeserializeAnimal(MemoryStream m) { }
}
```

### Typical Plant Structure

```csharp
using System.IO;
using OrganismBase;

[assembly: OrganismClass("MyNamespace.MyPlant")]
[assembly: AuthorInformation("Name", "email")]

namespace MyNamespace;

[MatureSize(24)]
[PlantSkin(PlantSkinFamily.Plant)]
[SeedSpreadDistance(0)]
[MarkingColor(KnownColor.Green)]
public class MyPlant : Plant
{
    public override void SerializePlant(MemoryStream m) { }
    public override void DeserializePlant(MemoryStream m) { }
}
```

## Common Patterns

### Scanning for Food

```csharp
ArrayList organisms = Scan();
foreach (OrganismState organism in organisms)
{
    if (organism is PlantState plant)
    {
        // Found a plant
    }
    else if (organism is AnimalState animal)
    {
        // Found an animal
    }
}
```

### Moving to Target

```csharp
if (!IsMoving)
{
    BeginMoving(new MovementVector(target.Position, speed));
}
```

### Eating

```csharp
if (CanEat && WithinEatingRange(target))
{
    BeginEating(target);
}
```

### Attacking

```csharp
if (WithinAttackingRange(target) && target.IsAlive)
{
    BeginAttacking(target);
}
```

### Reproducing

```csharp
if (CanReproduce)
{
    BeginReproduction(null);  // null = no DNA modification
}
```

## Best Practices

1. **Always use try-catch** in event handlers to prevent crashes
2. **Check action state** before starting new actions (`IsMoving`, `IsEating`, etc.)
3. **Validate targets** in `Load` event using `LookFor()`
4. **Never attack your own species** - use `IsMySpecies()` check
5. **Sum characteristic points to exactly 100** for animals
6. **Use `OrganismRandom`** for reproducible random numbers
7. **Write to trace** for debugging: `WriteTrace(message)`
8. **Keep `Idle` handler fast** - it runs every turn

## Additional Resources

- [Getting Started Tutorial](../tutorials/getting-started.md)
- [Sample Creatures Source](../../../src/Terrarium.Samples/)
- [Architecture Documentation](../../architecture/)
