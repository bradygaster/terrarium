# Terrarium.OrganismBase

Base library for creating creatures (animals and plants) in the .NET Terrarium ecosystem simulation game.

## Overview

Terrarium.OrganismBase provides the core types, attributes, and engine interfaces needed to build autonomous organisms that compete and cooperate in a peer-to-peer ecosystem simulation.

## Getting Started

### Install via dotnet new template

```bash
dotnet new install Terrarium.Templates
dotnet new terrarium-creature -n MyCreature
```

### Or reference directly

```bash
dotnet add package Terrarium.OrganismBase
```

## Creating a Creature

Every creature needs:

1. **Assembly attributes** to identify it:
```csharp
[assembly: OrganismClass("MyNamespace.MyCreature")]
[assembly: AuthorInformation("Your Name", "your.email@example.com")]
```

2. **Characteristic attributes** (100 points total for animals):
```csharp
[MatureSize(26)]
[MaximumSpeedPoints(25)]
[CamouflagePoints(25)]
[EyesightPoints(50)]
```

3. **Inherit from `Animal` or `Plant`**:
```csharp
public class MyCreature : Animal
{
    protected override void Initialize()
    {
        Idle += OnIdle;
    }
    
    private void OnIdle(object sender, IdleEventArgs e)
    {
        // Your creature's logic here
    }
}
```

## Sample Creatures

See [Terrarium.Samples](https://github.com/terrarium-game/terrarium/tree/main/src/Terrarium.Samples) for working examples:
- **SimpleHerbivore**: Finds and eats plants
- **SimpleCarnivore**: Hunts other animals
- **SimplePlant**: Basic stationary plant

## Documentation

For full documentation, visit the [Terrarium documentation](https://github.com/terrarium-game/terrarium/tree/main/docs).

## License

MIT License - see LICENSE for details.
