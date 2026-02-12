# SDK Documentation

Comprehensive documentation for developing Terrarium creatures using the OrganismBase API.

## Quick Start

**New to Terrarium?** Start here:

1. **[🚀 Getting Started Guide](./getting-started.md)** - Zero to deployed creature in 10 minutes
2. [Tutorial 1: Simple Plant](./tutorials/tutorial-1-simple-plant.md) - Your first organism
3. [Tutorial 2: Herbivore](./tutorials/tutorial-2-herbivore.md) - Mobile creatures that eat plants
4. [Tutorial 3: Carnivore](./tutorials/tutorial-3-carnivore.md) - Predators that hunt other animals

**Already familiar?** Jump to [API Reference](./api/README.md) or [Sample Code](#sample-code).

## API Reference

Complete reference documentation:
- [API Overview](./api/README.md) - Complete API index
- [Animal Class](./api/animal.md) - Mobile creature base class
- [Plant Class](./api/plant.md) - Stationary organism base class
- [Attributes Reference](./api/attributes.md) - All creature attributes

## Sample Code

Working examples in `src/Terrarium.Samples/`:
- **SimplePlant** - Basic plant that grows and reproduces
- **SimpleHerbivore** - Beetle that finds and eats plants
- **SimpleCarnivore** - Scorpion that hunts and kills prey

## Key Concepts

### Namespaces

```csharp
using OrganismBase;  // Core API (NOT Terrarium.OrganismBase)
```

### Assembly Attributes

Every creature requires:
```csharp
[assembly: OrganismClass("YourNamespace.YourClass.YourClass")]
[assembly: AuthorInformation("Your Name", "your.email@example.com")]
```

### Base Classes

- `Plant` - Stationary, automatic behavior
- `Animal` - Mobile, event-driven behavior
  - `[Carnivore(false)]` - Herbivore (eats plants)
  - `[Carnivore(true)]` - Carnivore (eats dead animals)

### Modern C#

Documentation uses .NET 10 and modern C# features:
- File-scoped namespaces
- Nullable reference types
- Pattern matching
- Records (where applicable)

## Building and Testing

Build the samples:
```bash
dotnet build src/Terrarium.Samples/SimpleHerbivore
dotnet build src/Terrarium.Samples/SimpleCarnivore
dotnet build src/Terrarium.Samples/SimplePlant
```

Or build all samples:
```bash
dotnet build src/Terrarium.sln
```

## Documentation Structure

```
docs/sdk/
├── README.md (this file)
├── tutorials/
│   ├── getting-started.md
│   ├── tutorial-1-simple-plant.md
│   ├── tutorial-2-herbivore.md
│   └── tutorial-3-carnivore.md
└── api/
    ├── README.md
    ├── animal.md
    ├── plant.md
    └── attributes.md
```

## Additional Resources

- [Architecture Documentation](../architecture/)
- [Sample Creatures Source](../../src/Terrarium.Samples/)
- [OrganismBase Source](../../src/Terrarium.OrganismBase/)

## Contributing

Found an error or have a suggestion? Open an issue or submit a pull request!
