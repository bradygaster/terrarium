# Plant Class API Reference

The `Plant` class is the base class for all stationary organisms in Terrarium.

## Namespace

```csharp
using OrganismBase;
```

## Declaration

```csharp
public abstract class Plant : Organism
```

## Overview

`Plant` provides automatic lifecycle management for stationary organisms:
- Automatic growth using light energy
- Automatic reproduction when mature
- Simple lifecycle with minimal code required

Plants are the **simplest organisms** to create - they require no event handlers or custom logic for basic functionality.

## Properties

### State

```csharp
public new PlantState State { get; }
```

Returns the plant's current state. Hides the base `OrganismState` property with the more specific `PlantState` type.

## Abstract Methods

### SerializePlant()

```csharp
public abstract void SerializePlant(MemoryStream m);
```

Override to serialize custom state data. Can be empty for stateless plants.

**Parameters**:
- `m` - Memory stream to write to

**Example**:
```csharp
public override void SerializePlant(MemoryStream m)
{
    // For simple plants: leave empty
}
```

### DeserializePlant()

```csharp
public abstract void DeserializePlant(MemoryStream m);
```

Override to deserialize custom state data. Can be empty for stateless plants.

**Parameters**:
- `m` - Memory stream to read from

**Example**:
```csharp
public override void DeserializePlant(MemoryStream m)
{
    // For simple plants: leave empty
}
```

## Automatic Behavior

Plants have built-in behaviors handled by the base class:

### Growth

Plants automatically grow from their initial size to `MatureSize`:
- Uses stored energy to grow
- Requires `EnergyState.Normal` or better
- Grows one radius unit at a time
- Energy cost: `EngineSettings.PlantRequiredEnergyPerUnitOfRadiusGrowth`

### Energy Generation

Plants automatically convert light to energy each turn:
- Maximum energy per tick: `EngineSettings.MaxEnergyFromLightPerTick` (550)
- Energy gained depends on light availability
- Plants compete for light when crowded
- Optimal light = 100% of available light

### Reproduction

Plants automatically reproduce when:
- `State.IsMature` is true (reached `MatureSize`)
- `State.ReadyToReproduce` is true (cooldown expired)
- `State.EnergyState >= EnergyState.Normal`

Reproduction:
- Creates offspring of the same species
- Offspring appears within `SeedSpreadDistance`
- Costs significant energy
- Has cooldown period based on size

### Healing

Plants automatically heal damage:
- Regrows food chunks when herbivores eat them
- Uses stored energy for healing
- Healing rate: `EngineSettings.PlantMaxHealingPerTickPerRadius` chunks per tick
- Energy cost per chunk: `EngineSettings.PlantRequiredEnergyPerUnitOfHealing`

### Lifecycle

Plants have a natural lifespan:
- Lifespan: `EngineSettings.PlantLifeSpanPerUnitMaximumRadius * MatureSize` ticks
- After lifespan expires, plant dies of old age
- Can also die from:
  - Complete food depletion (eaten by herbivores)
  - Energy starvation (insufficient light)

## Attributes

Plants are configured entirely through attributes:

### Required Attributes

```csharp
[MatureSize(24)]                            // Size when fully grown (25-48)
[PlantSkin(PlantSkinFamily.Plant)]          // Visual appearance
[MarkingColor(KnownColor.Green)]            // Color
[SeedSpreadDistance(0)]                     // Reproduction range (0-1000)
```

See [Attributes Reference](./attributes.md) for details.

## Usage Example

Complete plant implementation:

```csharp
using System.IO;
using OrganismBase;

[assembly: OrganismClass("MyCreatures.SimplePlant.SimplePlant")]
[assembly: AuthorInformation("Your Name", "email@example.com")]

namespace MyCreatures.SimplePlant;

/// <summary>
/// A simple plant that grows and reproduces automatically.
/// </summary>
[MatureSize(24)]
[PlantSkin(PlantSkinFamily.Plant)]
[SeedSpreadDistance(0)]
[MarkingColor(System.Drawing.KnownColor.Green)]
public class SimplePlant : Plant
{
    public override void SerializePlant(MemoryStream m) { }
    public override void DeserializePlant(MemoryStream m) { }
}
```

That's a complete, functional plant!

## Advanced Plant Patterns

### Large, Clustered Plant

```csharp
[MatureSize(40)]              // Large size
[PlantSkin(PlantSkinFamily.Plant)]
[SeedSpreadDistance(100)]     // Low spread = clusters
[MarkingColor(KnownColor.DarkGreen)]
```

Characteristics:
- Provides more food for herbivores
- Forms dense "forests"
- Requires more energy to maintain
- Reproduces more slowly

### Small, Fast-Spreading Plant

```csharp
[MatureSize(25)]              // Minimum size
[PlantSkin(PlantSkinFamily.Plant)]
[SeedSpreadDistance(1000)]    // Maximum spread
[MarkingColor(KnownColor.YellowGreen)]
```

Characteristics:
- Colonizes entire world quickly
- Requires less energy to maintain
- Provides less food per plant
- Reproduces more quickly

## PlantState Details

Plants have access to their state via the `State` property:

```csharp
PlantState state = State;
```

Key `PlantState` properties:
- `Height` - Current height (grows with radius)
- `FoodChunks` - Current food chunks available
- `CurrentMaxFoodChunks` - Maximum food chunks at current size
- `PercentInjured` - Damage percentage (1.0 = completely eaten)
- `IsAlive` - Still alive
- `IsMature` - Reached full size
- `Position` - World location
- `Radius` - Current size
- `EnergyState` - Energy level
- And more...

See [PlantState Reference](./state.md#plantstate) for complete details.

## Energy and Light

### Light Competition

Plants compete for light based on their position:
- Plants in open areas get full light
- Plants under other organisms get reduced light
- Dense plant populations reduce available light
- Height affects light collection (taller plants get more)

### Energy Balance

Plants must balance:
- **Energy gain** from light
- **Energy costs** for:
  - Baseline metabolism (`BasePlantEnergyPerUnitOfRadius` per tick)
  - Growth (`PlantRequiredEnergyPerUnitOfRadiusGrowth` per radius increase)
  - Healing (`PlantRequiredEnergyPerUnitOfHealing` per food chunk)
  - Reproduction (see `PlantIncubationEnergyPerUnitOfRadius`)

If energy drops too low:
- Plant stops growing
- Plant cannot reproduce
- Plant eventually dies

## Inherited from Organism

Plants inherit these members from the base `Organism` class:

### Properties

- `Position` - Current location (`Point`)
- `CanReproduce` - Can reproduce now (`Boolean`)
- `IsReproducing` - Currently reproducing (`Boolean`)
- `OrganismRandom` - Random number generator (`Random`)
- `ID` - Unique identifier (`string`)
- `TurnsSkipped` - Turns organism was too slow (`int`)

### Methods

- `BeginReproduction(byte[]? dna)` - Manually trigger reproduction (usually not needed)
- `DistanceTo(OrganismState)` - Calculate distance to organism
- `WriteTrace(object...)` - Write debug messages

See [Organism API Documentation](./organism.md) for details.

## Design Considerations

### Size Strategy

**Small plants (25-30)**:
- ✅ Grow quickly
- ✅ Reproduce quickly
- ✅ Low energy requirements
- ❌ Provide less food
- ❌ Easier to find and eat

**Large plants (40-48)**:
- ✅ Provide more food
- ✅ Harder to completely consume
- ✅ Better light competition (taller)
- ❌ Grow slowly
- ❌ Reproduce slowly
- ❌ High energy requirements

### Spread Strategy

**Clustered (0-200)**:
- ✅ Forms "forests" or patches
- ✅ Predictable food sources for herbivores
- ✅ Can monopolize good locations
- ❌ Intense light competition
- ❌ Vulnerable to local predation

**Scattered (500-1000)**:
- ✅ Colonizes entire world
- ✅ Less light competition
- ✅ Harder for herbivores to eliminate
- ❌ Less predictable food sources
- ❌ No territorial advantage

## Common Mistakes

### Wrong Namespace

```csharp
// ❌ Wrong
using Terrarium.OrganismBase;

// ✅ Correct
using OrganismBase;
```

### Invalid MatureSize

```csharp
// ❌ Too small
[MatureSize(20)]  // Minimum is 25

// ❌ Too large
[MatureSize(50)]  // Maximum is 48

// ✅ Valid
[MatureSize(25)]  // Minimum
[MatureSize(48)]  // Maximum
```

### Invalid SeedSpreadDistance

```csharp
// ❌ Too large
[SeedSpreadDistance(2000)]  // Maximum is 1000

// ✅ Valid
[SeedSpreadDistance(0)]     // Minimum
[SeedSpreadDistance(1000)]  // Maximum
```

### Trying to Add Event Handlers

```csharp
// ❌ Wrong - plants don't use events
public class MyPlant : Plant
{
    protected override void Initialize()
    {
        Idle += IdleEvent;  // Plants don't have Idle event!
    }
}

// ✅ Correct - plants need no initialization
public class MyPlant : Plant
{
    // No Initialize() needed!
    // Everything is automatic!
}
```

## See Also

- [Organism Class Reference](./organism.md)
- [PlantState Reference](./state.md#plantstate)
- [Attributes Reference](./attributes.md)
- [EngineSettings Reference](./engine-settings.md)
- [Tutorial: Creating a Simple Plant](../tutorials/tutorial-1-simple-plant.md)
