# Terrarium SDK Samples

Example creature implementations for .NET Terrarium, targeting .NET 10 and `Terrarium.OrganismBase`.

These samples show how to write creatures that live in the Terrarium ecosystem. Each sample is a standalone project you can build, study, and use as a starting point for your own creatures.

## Samples

### SimpleHerbivore

A plant-eating beetle that demonstrates the core herbivore gameplay loop:

- **Scanning** the environment with `Scan()` to find plants
- **Moving** toward targets with `BeginMoving()` and `MovementVector`
- **Eating** plants with `BeginEating()` and `WithinEatingRange()`
- **Reproducing** with `BeginReproduction()` when `CanReproduce` is true
- **Random wandering** when no food is in sight

**Strategy:** Invest all 100 attribute points into Camouflage (50) and Eyesight (50). Stay hidden and spot food from far away.

### SimpleCarnivore

A predatory scorpion that hunts, kills, and eats other animals:

- **Scanning** for prey with `Scan()`, filtering by `AnimalState`
- **Species identification** with `IsMySpecies()` to avoid attacking your own kind
- **Attacking** living prey with `BeginAttacking()` and `WithinAttackingRange()`
- **Eating** dead prey with `BeginEating()` (carnivores can only eat dead animals)
- **Pursuit** at maximum speed with `Species.MaximumSpeed`

**Strategy:** Heavy attack damage (52) and speed (28), with enough eyesight (20) to spot prey. Chase, kill, eat, repeat.

### SimplePlant

The simplest possible organism — a plant that just exists and reproduces:

- **Automatic reproduction** handled by the `Plant` base class
- **No event handlers needed** — plants reproduce when `CanReproduce` is true
- **Minimal code** — just attributes and serialization stubs

**Strategy:** Plants compete for space and light. The `MatureSize` attribute controls growth rate and reproductive speed.

## How Creature Attributes Work

Every creature uses attributes to define its identity and capabilities.

### Assembly-Level Attributes (required)

```csharp
[assembly: OrganismClass("MyNamespace.MyCreature")]  // The class that derives from Animal or Plant
[assembly: AuthorInformation("Your Name", "you@example.com")]
```

### Creature Type & Appearance

| Attribute | Description |
|-----------|-------------|
| `[Carnivore(bool)]` | `true` = eats dead animals, `false` = eats plants |
| `[MatureSize(int)]` | 24–48. Lower = faster reproduction, higher = more power |
| `[AnimalSkin(AnimalSkinFamily)]` | Ant, Beetle, Spider, Inchworm, or Scorpion |
| `[PlantSkin(PlantSkinFamily)]` | Plant, PlantOne, PlantTwo, or PlantThree |
| `[MarkingColor(KnownColor)]` | Color marking for identification |

### Point-Based Attributes (100 points total)

| Attribute | What It Does |
|-----------|--------------|
| `[MaximumEnergyPoints]` | Max energy storage |
| `[EatingSpeedPoints]` | How fast you eat |
| `[AttackDamagePoints]` | Damage dealt per attack |
| `[DefendDamagePoints]` | Damage dealt when defending |
| `[MaximumSpeedPoints]` | Top movement speed |
| `[CamouflagePoints]` | How well hidden you are |
| `[EyesightPoints]` | How far you can see |

You get exactly 100 points. Choose wisely based on your creature's strategy.

## Building

Each sample is a standalone `.csproj` that references `Terrarium.OrganismBase`:

```bash
dotnet build src/Terrarium.Samples/SimpleHerbivore/SimpleHerbivore.csproj
dotnet build src/Terrarium.Samples/SimpleCarnivore/SimpleCarnivore.csproj
dotnet build src/Terrarium.Samples/SimplePlant/SimplePlant.csproj
```

Or build all samples as part of the full solution:

```bash
dotnet build src/Terrarium.sln
```

## Creating Your Own Creature

1. Create a new class library targeting `net10.0`
2. Add a `ProjectReference` to `Terrarium.OrganismBase`
3. For an animal: derive from `Animal`, add assembly and class attributes, override `Initialize()`, `SerializeAnimal()`, and `DeserializeAnimal()`
4. For a plant: derive from `Plant`, add assembly and class attributes, override `SerializePlant()` and `DeserializePlant()`
5. Hook into events (`Load`, `Idle`, `Attacked`, `MoveCompleted`, etc.) in `Initialize()`
6. Build your creature as a `.dll` and load it into the Terrarium game
