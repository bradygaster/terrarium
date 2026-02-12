# Animal Class API Reference

The `Animal` class is the base class for all mobile creatures in Terrarium (herbivores and carnivores).

## Namespace

```csharp
using OrganismBase;
```

## Declaration

```csharp
public abstract class Animal : Organism
```

## Overview

`Animal` provides the core capabilities for mobile organisms:
- Movement through the world
- Scanning for other organisms
- Eating (plants or dead animals)
- Attacking other animals
- Defending against attacks
- Event-driven behavior model

All animals must:
1. Derive from `Animal`
2. Override `Initialize()` to register event handlers
3. Implement `SerializeAnimal()` and `DeserializeAnimal()`

## Properties

### State and Species

#### State

```csharp
public new AnimalState State { get; }
```

Returns the animal's current state. Hides the base `OrganismState` property with the more specific `AnimalState` type.

#### Species

```csharp
public IAnimalSpecies Species { get; }
```

Returns the animal's species information, including:
- Mature size
- Maximum speed
- Attack damage
- Defense damage
- Whether it's a carnivore
- And other species-specific characteristics

### World Information

#### WorldWidth

```csharp
public int WorldWidth { get; }
```

The width of the game world in pixels.

#### WorldHeight

```csharp
public int WorldHeight { get; }
```

The height of the game world in pixels.

### Movement

#### CurrentMoveToAction

```csharp
public MoveToAction? CurrentMoveToAction { get; }
```

Returns the current movement action, or `null` if not moving.

#### IsMoving

```csharp
public Boolean IsMoving { get; }
```

`true` if the animal is currently moving, `false` otherwise.

### Defense

#### CurrentDefendAction

```csharp
public DefendAction? CurrentDefendAction { get; }
```

Returns the current defense action, or `null` if not defending.

#### IsDefending

```csharp
public Boolean IsDefending { get; }
```

`true` if the animal is currently defending, `false` otherwise.

### Attack

#### CurrentAttackAction

```csharp
public AttackAction? CurrentAttackAction { get; }
```

Returns the current attack action, or `null` if not attacking.

#### IsAttacking

```csharp
public Boolean IsAttacking { get; }
```

`true` if the animal is currently attacking, `false` otherwise.

### Eating

#### CurrentEatAction

```csharp
public EatAction? CurrentEatAction { get; }
```

Returns the current eating action, or `null` if not eating.

#### IsEating

```csharp
public Boolean IsEating { get; }
```

`true` if the animal is currently eating, `false` otherwise.

#### CanEat

```csharp
public Boolean CanEat { get; }
```

`true` if the animal's energy state is Normal or below (hungry enough to eat).

### Communication

#### Antennas

```csharp
public AntennaState Antennas { get; set; }
```

Gets or sets the antenna state for inter-organism communication (advanced feature).

## Events

### Load

```csharp
public event LoadEventHandler? Load;
```

Fires **first** every turn, before any other events. Use this to:
- Validate tracked organisms still exist
- Refresh organism states via `LookFor()`
- Update internal tracking state

**Example**:
```csharp
protected override void Initialize()
{
    Load += LoadEvent;
}

private void LoadEvent(object sender, LoadEventArgs e)
{
    if (targetAnimal != null)
    {
        targetAnimal = (AnimalState?)LookFor(targetAnimal);
    }
}
```

### Idle

```csharp
public event IdleEventHandler? Idle;
```

Fires when the animal has **no actions in progress**. This is where you implement your behavior logic.

**Example**:
```csharp
private void IdleEvent(object sender, IdleEventArgs e)
{
    if (CanReproduce)
        BeginReproduction(null);
        
    if (CanEat && !IsEating)
    {
        FindFoodAndEat();
    }
}
```

### MoveCompleted

```csharp
public event MoveCompletedEventHandler? MoveCompleted;
```

Fires when a movement action completes.

**EventArgs**: `MoveCompletedEventArgs`
- `ReasonForStop` - Why movement stopped (Arrived, Blocked, Interrupted, etc.)

### AttackCompleted

```csharp
public event AttackCompletedEventHandler? AttackCompleted;
```

Fires when an attack action completes.

**EventArgs**: `AttackCompletedEventArgs`
- Details about the completed attack

### EatCompleted

```csharp
public event EatCompletedEventHandler? EatCompleted;
```

Fires when an eating action completes.

**EventArgs**: `EatCompletedEventArgs`
- Information about the eating session

### DefendCompleted

```csharp
public event DefendCompletedEventHandler? DefendCompleted;
```

Fires when a defense action completes.

**EventArgs**: `DefendCompletedEventArgs`
- Defense result information

### ReproduceCompleted

```csharp
public event ReproduceCompletedEventHandler? ReproduceCompleted;
```

Fires when reproduction completes successfully.

**EventArgs**: `ReproduceCompletedEventArgs`
- Information about the offspring

### Attacked

```csharp
public event AttackedEventHandler? Attacked;
```

Fires when another animal attacks you.

**EventArgs**: `AttackedEventArgs`
- `Attacker` - The `AnimalState` of the attacking animal
- Use this to implement counterattack or flee behavior

**Example**:
```csharp
private void AttackedEvent(object sender, AttackedEventArgs e)
{
    AnimalState attacker = e.Attacker;
    
    if (CanAttack(attacker))
    {
        // Fight back
        targetEnemy = attacker;
    }
}
```

### Born

```csharp
public event BornEventHandler? Born;
```

Fires once when the animal is first created.

**EventArgs**: `BornEventArgs`

### Teleported

```csharp
public event TeleportedEventHandler? Teleported;
```

Fires when the engine teleports the animal (rare).

**EventArgs**: `TeleportedEventArgs`

## Methods

### Scanning and Detection

#### Scan()

```csharp
public ArrayList Scan()
```

Scans the area within your eyesight range and returns all visible organisms.

**Returns**: `ArrayList` of `OrganismState` objects (`PlantState` or `AnimalState`)

**Eyesight range** is determined by:
- Base eyesight (5 unit radius)
- `EyesightPoints` characteristic
- Target organism's `CamouflagePoints`

**Example**:
```csharp
ArrayList foundOrganisms = Scan();
foreach (OrganismState organism in foundOrganisms)
{
    if (organism is PlantState plant)
    {
        // Found a plant
    }
    else if (organism is AnimalState animal && !IsMySpecies(animal))
    {
        // Found another species
    }
}
```

#### LookFor()

```csharp
public OrganismState? LookFor(OrganismState organismState)
```

Attempts to find and return an updated state for a specific organism you're tracking.

**Parameters**:
- `organismState` - The organism to look for

**Returns**: Updated `OrganismState`, or `null` if:
- The organism died
- The organism moved out of range
- The organism's camouflage hides it from you

**Throws**:
- `ArgumentNullException` if `organismState` is null

**Example**:
```csharp
// In Load event:
if (targetPlant != null)
{
    targetPlant = (PlantState?)LookFor(targetPlant);
    // targetPlant is now null if it's gone
}
```

#### RefreshState()

```csharp
public OrganismState? RefreshState(string organismID)
```

Looks for an organism by its ID string.

**Parameters**:
- `organismID` - The unique ID of the organism

**Returns**: Updated `OrganismState`, or `null` if not found

**Throws**:
- `ArgumentNullException` if `organismID` is null

#### IsMySpecies()

```csharp
public Boolean IsMySpecies(OrganismState targetState)
```

Checks if another organism is the same species as you.

**Parameters**:
- `targetState` - The organism to check

**Returns**: `true` if same species, `false` otherwise

**Throws**:
- `ArgumentNullException` if `targetState` is null

**Critical**: Always use this before attacking! Attacking your own species leads to population collapse.

**Example**:
```csharp
foreach (OrganismState organism in Scan())
{
    if (organism is AnimalState animal && !IsMySpecies(animal))
    {
        // Safe to target - different species
    }
}
```

### Movement

#### BeginMoving()

```csharp
public void BeginMoving(MovementVector vector)
```

Starts moving toward a destination.

**Parameters**:
- `vector` - `MovementVector` specifying destination and speed

**Throws**:
- `ArgumentNullException` if `vector` is null
- `TooFastException` if speed exceeds `Species.MaximumSpeed`
- `OutOfBoundsException` if destination is outside world bounds

**Example**:
```csharp
if (!IsMoving)
{
    BeginMoving(new MovementVector(target.Position, 2));
}
```

Speed considerations:
- Higher speed costs more energy
- Speed limited by `MaximumSpeedPoints` characteristic
- Use `Species.MaximumSpeed` for fastest movement

#### StopMoving()

```csharp
public void StopMoving()
```

Immediately stops any current movement.

**Example**:
```csharp
if (IsMoving)
{
    StopMoving();
}
```

### Eating

#### BeginEating()

```csharp
public void BeginEating(OrganismState targetOrganism)
```

Starts eating an organism.

**Parameters**:
- `targetOrganism` - The organism to eat (`PlantState` or `AnimalState`)

**Throws**:
- `ArgumentNullException` if `targetOrganism` is null
- `AlreadyFullException` if energy state is above Normal
- `NotVisibleException` if target is not visible (camouflaged or out of range)
- `NotWithinDistanceException` if not within eating range
- `ImproperFoodException` if food type is wrong:
  - Herbivores cannot eat animals
  - Carnivores cannot eat plants
- `NotDeadException` if carnivore tries to eat living animal

**Herbivore rules**:
- Can only eat plants (`PlantState`)
- Plant must be visible and within range

**Carnivore rules**:
- Can only eat dead animals (`!IsAlive`)
- Cannot eat plants
- Cannot eat own species (checked separately)

**Example**:
```csharp
if (CanEat && WithinEatingRange(targetPlant))
{
    try
    {
        BeginEating(targetPlant);
    }
    catch (Exception exc)
    {
        WriteTrace(exc.ToString());
    }
}
```

#### WithinEatingRange()

```csharp
public Boolean WithinEatingRange(OrganismState targetOrganism)
```

Checks if an organism is close enough to eat.

**Parameters**:
- `targetOrganism` - The organism to check

**Returns**: `true` if adjacent or overlapping, `false` otherwise

**Throws**:
- `ArgumentNullException` if `targetOrganism` is null

### Attacking

#### BeginAttacking()

```csharp
public void BeginAttacking(AnimalState targetAnimal)
```

Starts attacking another animal.

**Parameters**:
- `targetAnimal` - The animal to attack

**Throws**:
- `ArgumentNullException` if `targetAnimal` is null
- `NotHungryException` if herbivore cannot attack (see `CanAttack()`)

**Example**:
```csharp
if (WithinAttackingRange(targetAnimal) && targetAnimal.IsAlive)
{
    BeginAttacking(targetAnimal);
}
```

#### WithinAttackingRange()

```csharp
public Boolean WithinAttackingRange(AnimalState targetOrganism)
```

Checks if an animal is close enough to attack.

**Parameters**:
- `targetOrganism` - The animal to check

**Returns**: `true` if within 1 unit radius, `false` otherwise

**Throws**:
- `ArgumentNullException` if `targetOrganism` is null

**Note**: Attack range is slightly larger than eating range.

#### CanAttack()

```csharp
public Boolean CanAttack(AnimalState targetAnimal)
```

Checks if you're allowed to attack a specific animal.

**Parameters**:
- `targetAnimal` - The animal to check

**Returns**: 
- **Carnivores**: Always `true`
- **Herbivores**: `true` if:
  - You were previously attacked by that animal, OR
  - Your energy state is Hungry or worse

**Throws**:
- `ArgumentNullException` if `targetAnimal` is null

### Defending

#### BeginDefending()

```csharp
public void BeginDefending(AnimalState targetAnimal)
```

Starts defending against a specific attacker.

**Parameters**:
- `targetAnimal` - The attacking animal to defend against

**Throws**:
- `ArgumentNullException` if `targetAnimal` is null

## Abstract Methods

### SerializeAnimal()

```csharp
public abstract void SerializeAnimal(MemoryStream m);
```

Override to serialize custom state data.

**Parameters**:
- `m` - Memory stream to write to

**Example**:
```csharp
public override void SerializeAnimal(MemoryStream m)
{
    // For stateless animals:
    // (empty implementation)
    
    // For animals with custom state:
    // using BinaryWriter writer = new BinaryWriter(m);
    // writer.Write(myCustomData);
}
```

### DeserializeAnimal()

```csharp
public abstract void DeserializeAnimal(MemoryStream m);
```

Override to deserialize custom state data.

**Parameters**:
- `m` - Memory stream to read from

**Example**:
```csharp
public override void DeserializeAnimal(MemoryStream m)
{
    // For stateless animals:
    // (empty implementation)
    
    // For animals with custom state:
    // using BinaryReader reader = new BinaryReader(m);
    // myCustomData = reader.ReadInt32();
}
```

## Inherited from Organism

Animals inherit these members from the base `Organism` class:

### Properties

- `Position` - Current location (`Point`)
- `CanReproduce` - Can reproduce now (`Boolean`)
- `IsReproducing` - Currently reproducing (`Boolean`)
- `OrganismRandom` - Random number generator (`Random`)
- `ID` - Unique identifier (`string`)
- `TurnsSkipped` - Turns organism was too slow (`int`)

### Methods

- `BeginReproduction(byte[]? dna)` - Start reproduction
- `DistanceTo(OrganismState)` - Calculate distance to organism
- `WriteTrace(object...)` - Write debug messages

See [Organism API Documentation](./organism.md) for details.

## Usage Example

Complete herbivore implementation:

```csharp
using System;
using System.Collections;
using System.Drawing;
using System.IO;
using OrganismBase;

[assembly: OrganismClass("MyCreatures.SimpleHerbivore.SimpleHerbivore")]
[assembly: AuthorInformation("Your Name", "email@example.com")]

namespace MyCreatures.SimpleHerbivore;

[Carnivore(false)]
[MatureSize(26)]
[AnimalSkin(AnimalSkinFamily.Beetle)]
[MarkingColor(KnownColor.Green)]
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
                    else if (!IsMoving)
                    {
                        BeginMoving(new MovementVector(targetPlant.Position, 2));
                    }
                }
                else if (!ScanForPlant() && !IsMoving)
                {
                    WanderRandomly();
                }
            }
        }
        catch (Exception exc)
        {
            WriteTrace(exc.ToString());
        }
    }

    private bool ScanForPlant()
    {
        ArrayList found = Scan();
        foreach (OrganismState organism in found)
        {
            if (organism is PlantState plant)
            {
                targetPlant = plant;
                BeginMoving(new MovementVector(plant.Position, 2));
                return true;
            }
        }
        return false;
    }

    private void WanderRandomly()
    {
        int x = OrganismRandom.Next(0, WorldWidth - 1);
        int y = OrganismRandom.Next(0, WorldHeight - 1);
        BeginMoving(new MovementVector(new Point(x, y), 2));
    }

    public override void SerializeAnimal(MemoryStream m) { }
    public override void DeserializeAnimal(MemoryStream m) { }
}
```

## See Also

- [Organism Class Reference](./organism.md)
- [AnimalState Reference](./state.md#animalstate)
- [Events Reference](./events.md)
- [Attributes Reference](./attributes.md)
- [Tutorial: Creating a Herbivore](../tutorials/tutorial-2-herbivore.md)
- [Tutorial: Creating a Carnivore](../tutorials/tutorial-3-carnivore.md)
