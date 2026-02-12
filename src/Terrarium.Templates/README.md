# Terrarium Templates

dotnet new templates for creating creatures in the .NET Terrarium ecosystem simulation game.

## Installation

```bash
dotnet new install Terrarium.Templates
```

## Usage

### Create a new creature project

```bash
# Create an herbivore (default)
dotnet new terrarium-creature -n MyHerbivore

# Create a carnivore
dotnet new terrarium-creature -n MyCarnivore --IsCarnivore true

# Create a plant
dotnet new terrarium-creature -n MyPlant --CreatureType Plant

# Specify author information
dotnet new terrarium-creature -n MyCreature --AuthorName "Your Name" --AuthorEmail "you@example.com"
```

### Template Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `--CreatureType` | Animal or Plant | Animal |
| `--IsCarnivore` | Whether animal eats meat (vs plants) | false |
| `--AuthorName` | Creature author name | Anonymous |
| `--AuthorEmail` | Creature author email | author@example.com |
| `--Framework` | Target framework | net10.0 |

## What Gets Created

The template creates:
- A `.csproj` file with reference to `Terrarium.OrganismBase`
- A creature source file with:
  - Assembly attributes (`OrganismClass`, `AuthorInformation`)
  - Characteristic attributes (size, points, appearance)
  - Starter implementation with event handlers (for animals) or minimal code (for plants)
  - TODO comments guiding implementation
- A `README.md` with usage instructions

## Next Steps

After creating your creature:
1. Open the project in your IDE
2. Customize the characteristic attributes (animals get 100 points to distribute)
3. Implement behavior in the event handlers
4. Build: `dotnet build`
5. Deploy the DLL to your Terrarium client

## Resources

- [Terrarium Documentation](https://github.com/terrarium-game/terrarium/tree/main/docs)
- [Sample Creatures](https://github.com/terrarium-game/terrarium/tree/main/src/Terrarium.Samples)
- [OrganismBase API Reference](https://github.com/terrarium-game/terrarium/tree/main/src/Terrarium.OrganismBase)

## License

MIT License - see LICENSE for details.
