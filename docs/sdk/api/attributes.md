# Attributes Reference

Complete reference for all OrganismBase attributes used to define creature characteristics.

## Namespace

```csharp
using OrganismBase;
using System.Drawing;  // For KnownColor
```

## Assembly Attributes

### OrganismClassAttribute

**Required for all organisms**

```csharp
[assembly: OrganismClass("Namespace.ClassName")]
```

Identifies your creature to the Terrarium engine. Must match your class's fully-qualified name **exactly**.

**Parameters**:
- `className` (string) - Fully qualified class name

**Example**:
```csharp
[assembly: OrganismClass("MyCreatures.GreenBeetle.GreenBeetle")]

namespace MyCreatures.GreenBeetle;

public class GreenBeetle : Animal
{
    // ...
}
```

**Common mistake**: Including "Terrarium" in the namespace
```csharp
// ❌ Wrong
[assembly: OrganismClass("Terrarium.OrganismBase.MyCreature.MyCreature")]

// ✅ Correct
[assembly: OrganismClass("MyCreatures.MyCreature.MyCreature")]
```

### AuthorInformationAttribute

**Required for all organisms**

```csharp
[assembly: AuthorInformation("Author Name", "email@example.com")]
```

Credits the creature's creator.

**Parameters**:
- `authorName` (string) - Your name or handle
- `authorEmail` (string) - Your email address

**Example**:
```csharp
[assembly: AuthorInformation("Jane Smith", "jane.smith@example.com")]
```

## Universal Attributes

These apply to both animals and plants.

### MatureSizeAttribute

**Required**

```csharp
[MatureSize(size)]
```

Defines the organism's radius when fully grown.

**Parameters**:
- `matureSize` (int) - Size in pixels
  - Minimum: 25
  - Maximum: 48

**Affects**:
- Visual size in game
- Energy requirements
- Lifespan
- Reproduction cooldown
- (Animals) Attack/defense damage
- (Plants) Food provided to herbivores

**Examples**:
```csharp
[MatureSize(25)]  // Small, minimum size
[MatureSize(35)]  // Medium
[MatureSize(48)]  // Large, maximum size
```

### MarkingColorAttribute

**Required**

```csharp
[MarkingColor(color)]
```

Sets the organism's color.

**Parameters**:
- `color` (KnownColor) - Color from `System.Drawing.KnownColor` enum

**Popular colors**:
```csharp
[MarkingColor(KnownColor.Green)]
[MarkingColor(KnownColor.Red)]
[MarkingColor(KnownColor.Blue)]
[MarkingColor(KnownColor.Yellow)]
[MarkingColor(KnownColor.Orange)]
[MarkingColor(KnownColor.Purple)]
[MarkingColor(KnownColor.Brown)]
[MarkingColor(KnownColor.Black)]
[MarkingColor(KnownColor.White)]
[MarkingColor(KnownColor.Gray)]
[MarkingColor(KnownColor.DarkGreen)]
[MarkingColor(KnownColor.LightBlue)]
```

See [.NET KnownColor documentation](https://docs.microsoft.com/en-us/dotnet/api/system.drawing.knowncolor) for complete list.

## Animal-Only Attributes

### CarnivoreAttribute

**Required for animals**

```csharp
[Carnivore(isCarnivore)]
```

Defines whether the animal is a carnivore or herbivore.

**Parameters**:
- `isCarnivore` (bool)
  - `false` - Herbivore (eats plants)
  - `true` - Carnivore (eats dead animals)

**Effects of being a carnivore**:
- Lifespan: 2x longer than herbivores
- Can only eat dead animals (not plants)
- Can always attack other animals
- Attack/defense multiplier: 2x

**Examples**:
```csharp
[Carnivore(false)]  // Herbivore
[Carnivore(true)]   // Carnivore
```

### AnimalSkinAttribute

**Required for animals**

```csharp
[AnimalSkin(skinFamily)]
```

Determines the animal's visual appearance.

**Parameters**:
- `skinFamily` (AnimalSkinFamily) - Visual style

**Available skins**:
```csharp
[AnimalSkin(AnimalSkinFamily.Beetle)]
[AnimalSkin(AnimalSkinFamily.Scorpion)]
// Additional skins available in the engine
```

### Characteristic Point Attributes

**Animals have exactly 100 points to distribute** across seven characteristics. All seven attributes are required, even if set to 0.

**Total must equal 100!**

#### MaximumEnergyPointsAttribute

```csharp
[MaximumEnergyPoints(points)]
```

Energy storage capacity.

**Parameters**:
- `points` (int) - Points allocated (0-100)

**Effects**:
- Increases maximum energy storage
- Allows longer periods without food
- Helps survive lean times or long chases

**Strategy**:
- Endurance hunters: 20-30 points
- Most creatures: 0-10 points
- Energy efficiency builds: 0 points

#### EatingSpeedPointsAttribute

```csharp
[EatingSpeedPoints(points)]
```

How quickly the animal consumes food.

**Parameters**:
- `points` (int) - Points allocated (0-100)

**Effects**:
- Faster energy recovery
- Less time vulnerable while eating
- More efficient grazing

**Strategy**:
- Quick grazers: 30-50 points
- Carnivores: Usually 0 (prey doesn't escape)
- Most creatures: 0-20 points

#### AttackDamagePointsAttribute

```csharp
[AttackDamagePoints(points)]
```

Damage dealt when attacking.

**Parameters**:
- `points` (int) - Points allocated (0-100)

**Effects**:
- Higher damage per attack
- Faster kills
- More efficient hunting

**Strategy**:
- Carnivores: 40-70 points (essential)
- Herbivores: 0-20 points (defensive)
- Ambush predators: 60-80 points

#### DefendDamagePointsAttribute

```csharp
[DefendDamagePoints(points)]
```

Damage resistance when attacked.

**Parameters**:
- `points` (int) - Points allocated (0-100)

**Effects**:
- Reduces damage taken
- Increases survivability
- Allows tanking hits

**Strategy**:
- Tanks: 30-50 points
- Counter-attackers: 20-30 points
- Most creatures: 0-20 points
- Fast/stealthy builds: 0 points

#### MaximumSpeedPointsAttribute

```csharp
[MaximumSpeedPoints(points)]
```

Movement speed.

**Parameters**:
- `points` (int) - Points allocated (0-100)

**Effects**:
- Faster movement
- Can catch or escape prey/predators
- Higher energy cost for movement

**Strategy**:
- Pursuit predators: 30-50 points
- Escape artists: 30-50 points
- Ambush predators: 0 points
- Most builds: 10-30 points

#### CamouflagePointsAttribute

```csharp
[CamouflagePoints(points)]
```

Hiding ability from other organisms' vision.

**Parameters**:
- `points` (int) - Points allocated (0-100)

**Effects**:
- Harder for predators to detect
- Harder for prey to detect (carnivores)
- Increases survival rate

**Formula**: Camouflage reduces detection probability vs eyesight

**Strategy**:
- Stealth herbivores: 40-60 points
- Ambush predators: 30-50 points
- Active hunters: 0-20 points

#### EyesightPointsAttribute

```csharp
[EyesightPoints(points)]
```

Detection range for scanning.

**Parameters**:
- `points` (int) - Points allocated (0-100)

**Effects**:
- Larger scan radius
- Can detect food/threats earlier
- Better at finding camouflaged organisms

**Formula**: Base eyesight is 5 unit radius; points increase this

**Strategy**:
- Scouts: 40-60 points
- Most creatures: 20-50 points
- Ambush predators: 0-20 points

### Example Characteristic Distributions

**Stealth Herbivore**:
```csharp
[MaximumEnergyPoints(0)]
[EatingSpeedPoints(0)]
[AttackDamagePoints(0)]
[DefendDamagePoints(0)]
[MaximumSpeedPoints(0)]
[CamouflagePoints(50)]
[EyesightPoints(50)]
// Total: 100
```

**Pursuit Carnivore**:
```csharp
[MaximumEnergyPoints(0)]
[EatingSpeedPoints(0)]
[AttackDamagePoints(50)]
[DefendDamagePoints(0)]
[MaximumSpeedPoints(40)]
[CamouflagePoints(0)]
[EyesightPoints(10)]
// Total: 100
```

**Tank Build**:
```csharp
[MaximumEnergyPoints(20)]
[EatingSpeedPoints(0)]
[AttackDamagePoints(40)]
[DefendDamagePoints(30)]
[MaximumSpeedPoints(10)]
[CamouflagePoints(0)]
[EyesightPoints(0)]
// Total: 100
```

**Endurance Hunter**:
```csharp
[MaximumEnergyPoints(30)]
[EatingSpeedPoints(0)]
[AttackDamagePoints(30)]
[DefendDamagePoints(0)]
[MaximumSpeedPoints(40)]
[CamouflagePoints(0)]
[EyesightPoints(0)]
// Total: 100
```

## Plant-Only Attributes

### PlantSkinAttribute

**Required for plants**

```csharp
[PlantSkin(skinFamily)]
```

Determines the plant's visual appearance.

**Parameters**:
- `skinFamily` (PlantSkinFamily) - Visual style

**Available skins**:
```csharp
[PlantSkin(PlantSkinFamily.Plant)]  // Standard plant appearance
```

### SeedSpreadDistanceAttribute

**Required for plants**

```csharp
[SeedSpreadDistance(distance)]
```

How far seeds can spread when reproducing.

**Parameters**:
- `distance` (int) - Maximum distance in pixels
  - Minimum: 0
  - Maximum: 1000

**Effects**:
- 0 = Offspring appear immediately adjacent (clusters/forests)
- 500 = Moderate spread
- 1000 = Maximum spread (colonizes entire world)

**Examples**:
```csharp
[SeedSpreadDistance(0)]     // Clustered
[SeedSpreadDistance(500)]   // Scattered
[SeedSpreadDistance(1000)]  // Maximum dispersal
```

## Complete Examples

### Complete Plant

```csharp
using System.IO;
using OrganismBase;

[assembly: OrganismClass("MyCreatures.GreenPlant.GreenPlant")]
[assembly: AuthorInformation("Jane Smith", "jane@example.com")]

namespace MyCreatures.GreenPlant;

[MatureSize(30)]
[PlantSkin(PlantSkinFamily.Plant)]
[SeedSpreadDistance(500)]
[MarkingColor(System.Drawing.KnownColor.Green)]
public class GreenPlant : Plant
{
    public override void SerializePlant(MemoryStream m) { }
    public override void DeserializePlant(MemoryStream m) { }
}
```

### Complete Herbivore

```csharp
using System;
using System.Collections;
using System.IO;
using OrganismBase;

[assembly: OrganismClass("MyCreatures.BlueBeetle.BlueBeetle")]
[assembly: AuthorInformation("Jane Smith", "jane@example.com")]

namespace MyCreatures.BlueBeetle;

[Carnivore(false)]                    // Herbivore
[MatureSize(26)]
[AnimalSkin(AnimalSkinFamily.Beetle)]
[MarkingColor(System.Drawing.KnownColor.Blue)]
[MaximumEnergyPoints(0)]
[EatingSpeedPoints(0)]
[AttackDamagePoints(0)]
[DefendDamagePoints(0)]
[MaximumSpeedPoints(30)]
[CamouflagePoints(40)]
[EyesightPoints(30)]
// Total: 100 ✓
public class BlueBeetle : Animal
{
    protected override void Initialize()
    {
        Idle += IdleEvent;
    }

    private void IdleEvent(object sender, IdleEventArgs e)
    {
        // Behavior here
    }

    public override void SerializeAnimal(MemoryStream m) { }
    public override void DeserializeAnimal(MemoryStream m) { }
}
```

### Complete Carnivore

```csharp
using System;
using System.Collections;
using System.IO;
using OrganismBase;

[assembly: OrganismClass("MyCreatures.RedScorpion.RedScorpion")]
[assembly: AuthorInformation("Jane Smith", "jane@example.com")]

namespace MyCreatures.RedScorpion;

[Carnivore(true)]                       // Carnivore
[MatureSize(32)]
[AnimalSkin(AnimalSkinFamily.Scorpion)]
[MarkingColor(System.Drawing.KnownColor.Red)]
[MaximumEnergyPoints(10)]
[EatingSpeedPoints(0)]
[AttackDamagePoints(45)]
[DefendDamagePoints(10)]
[MaximumSpeedPoints(30)]
[CamouflagePoints(0)]
[EyesightPoints(5)]
// Total: 100 ✓
public class RedScorpion : Animal
{
    protected override void Initialize()
    {
        Idle += IdleEvent;
    }

    private void IdleEvent(object sender, IdleEventArgs e)
    {
        // Behavior here
    }

    public override void SerializeAnimal(MemoryStream m) { }
    public override void DeserializeAnimal(MemoryStream m) { }
}
```

## Validation

The Terrarium engine validates attributes when loading creatures:

### Assembly Attributes

- ✅ `[OrganismClass]` must exactly match the class's fully-qualified name
- ✅ `[AuthorInformation]` must be present

### Size Validation

- ✅ `MatureSize` must be between 25 and 48

### Animal Validation

- ✅ All seven characteristic point attributes must be present
- ✅ Characteristic points must sum to exactly 100
- ✅ `Carnivore` attribute must be present

### Plant Validation

- ✅ `SeedSpreadDistance` must be between 0 and 1000
- ✅ `PlantSkin` attribute must be present

## See Also

- [Animal Class Reference](./animal.md)
- [Plant Class Reference](./plant.md)
- [EngineSettings Reference](./engine-settings.md)
- [Tutorial: Creating a Plant](../tutorials/tutorial-1-simple-plant.md)
- [Tutorial: Creating a Herbivore](../tutorials/tutorial-2-herbivore.md)
- [Tutorial: Creating a Carnivore](../tutorials/tutorial-3-carnivore.md)
