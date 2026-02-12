# Tutorial 1: Creating a Simple Plant

> **New to Terrarium?** Start with the [Getting Started Guide](../getting-started.md) for a 10-minute quickstart!

Plants are the foundation of the Terrarium ecosystem. They convert light into energy and serve as food for herbivores. In this tutorial, you'll create a simple plant organism.

## Overview

Plants are the simplest organisms in Terrarium:
- They automatically reproduce when mature
- They grow by converting light energy
- They don't need custom logic or event handlers
- They provide food for herbivores

## The Simplest Plant

Here's a complete, working plant implementation:

```csharp
using System.IO;
using OrganismBase;

// Assembly-level attributes identify this creature
[assembly: OrganismClass("MyCreatures.GreenLeaf.GreenLeaf")]
[assembly: AuthorInformation("Your Name", "your.email@example.com")]

namespace MyCreatures.GreenLeaf;

/// <summary>
/// A simple plant that grows and reproduces automatically.
/// </summary>
[MatureSize(24)]
[PlantSkin(PlantSkinFamily.Plant)]
[SeedSpreadDistance(0)]
[MarkingColor(System.Drawing.KnownColor.Green)]
public class GreenLeaf : Plant
{
    public override void SerializePlant(MemoryStream m) { }
    public override void DeserializePlant(MemoryStream m) { }
}
```

That's it! This is a fully functional plant. Let's break down what each part does.

## Understanding Plant Attributes

Plants use attributes to define their characteristics:

### Required Attributes

#### `[MatureSize(size)]`
Defines the plant's size when fully grown (radius in pixels).
- Minimum: 25
- Maximum: 48
- Larger plants are more visible but require more energy

```csharp
[MatureSize(24)]  // A medium-sized plant
```

#### `[PlantSkin(family)]`
Determines the plant's appearance. Available families:
- `PlantSkinFamily.Plant` - Standard plant appearance

```csharp
[PlantSkin(PlantSkinFamily.Plant)]
```

#### `[MarkingColor(color)]`
Sets the plant's color using `System.Drawing.KnownColor`:

```csharp
[MarkingColor(KnownColor.Green)]      // Classic green
[MarkingColor(KnownColor.Brown)]       // Earthy brown
[MarkingColor(KnownColor.Yellow)]      // Bright yellow
[MarkingColor(KnownColor.DarkGreen)]   // Forest green
```

#### `[SeedSpreadDistance(distance)]`
How far seeds can spread when reproducing.
- Minimum: 0 (reproduces nearby)
- Maximum: 1000 (spreads across the world)

```csharp
[SeedSpreadDistance(0)]      // Clusters together
[SeedSpreadDistance(500)]    // Moderate spread
[SeedSpreadDistance(1000)]   // Maximum spread
```

## Serialization Methods

The `Plant` base class requires two serialization methods:

```csharp
public override void SerializePlant(MemoryStream m) { }
public override void DeserializePlant(MemoryStream m) { }
```

For simple plants without state, these can be empty. If your plant tracks custom data between turns, you'd serialize it here (covered in advanced tutorials).

## How Plants Work

### Automatic Behavior

The `Plant` base class handles everything automatically:

1. **Growth**: Plants grow from small to `MatureSize` using light energy
2. **Energy**: Plants convert available light into stored energy each turn
3. **Reproduction**: Once mature and with sufficient energy, plants automatically reproduce
4. **Lifecycle**: Plants have a lifespan based on their mature size

### Energy Management

Plants gain energy from light automatically:
- More light = more energy
- Plants in crowded areas compete for light
- Insufficient energy prevents growth and reproduction

## Building and Testing

### Build Your Plant

```bash
dotnet build
```

### Introduce to Terrarium

1. Build your project to produce the assembly DLL
2. Copy the DLL to Terrarium's organisms folder
3. Launch Terrarium
4. Click "Introduce Animal" and select your plant
5. Choose a location and watch it grow!

## Variations and Experimentation

Try these variations to understand plant behavior:

### Large, Slow-Spreading Plant

```csharp
[MatureSize(40)]
[PlantSkin(PlantSkinFamily.Plant)]
[SeedSpreadDistance(100)]
[MarkingColor(KnownColor.DarkGreen)]
```

Characteristics:
- Larger size = more food for herbivores
- Low spread distance = forms dense clusters
- More energy required to maintain

### Small, Fast-Spreading Plant

```csharp
[MatureSize(25)]
[PlantSkin(PlantSkinFamily.Plant)]
[SeedSpreadDistance(1000)]
[MarkingColor(KnownColor.YellowGreen)]
```

Characteristics:
- Minimum size = grows quickly
- Maximum spread = colonizes the entire world
- Requires less energy to maintain

### Colorful Display

```csharp
[MatureSize(30)]
[PlantSkin(PlantSkinFamily.Plant)]
[SeedSpreadDistance(500)]
[MarkingColor(KnownColor.Purple)]
```

## Strategy Considerations

When designing plants, consider:

1. **Size vs. Reproduction**: Larger plants provide more food but reproduce more slowly
2. **Spread Pattern**: Clustered plants (low spread) create "forests"; scattered plants (high spread) colonize more territory
3. **Color**: Use color to identify your plants and track their spread
4. **Energy Balance**: The engine automatically balances energy needs based on size

## Common Mistakes

### Wrong Assembly Attribute

```csharp
// ❌ Wrong - includes "Terrarium" prefix
[assembly: OrganismClass("Terrarium.OrganismBase.MyPlant.MyPlant")]

// ✅ Correct - matches actual namespace
[assembly: OrganismClass("MyCreatures.MyPlant.MyPlant")]
```

### Missing Namespace Reference

```csharp
// ❌ Wrong
using Terrarium.OrganismBase;

// ✅ Correct
using OrganismBase;
```

### Invalid Size Values

```csharp
// ❌ Too small
[MatureSize(20)]  // Minimum is 25

// ❌ Too large
[MatureSize(50)]  // Maximum is 48

// ✅ Valid range
[MatureSize(25)]  // Minimum
[MatureSize(48)]  // Maximum
```

## Next Steps

Now that you've created a plant, you're ready to create animals that eat them:

- [Tutorial 2: Creating a Herbivore](./tutorial-2-herbivore.md)

## Reference

- [Plant Class API Documentation](../api/plant.md)
- [Plant Attributes Reference](../api/attributes.md#plant-attributes)
- [EngineSettings Constants](../api/engine-settings.md)
