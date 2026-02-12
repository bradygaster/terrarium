# Getting Started with Terrarium Creature Development

> **⚠️ New Guide Available:** This is the conceptual introduction. For a hands-on 10-minute quickstart, see **[Getting Started (Quick)](../getting-started.md)** instead!

Welcome to Terrarium creature development! This tutorial will guide you through the concepts and project structure for creating organisms using modern C# and the OrganismBase API.

## Prerequisites

- Visual Studio 2025 or later with .NET 10 SDK
- Basic understanding of C# and object-oriented programming
- Terrarium installed and running

## What You'll Build

In this tutorial series, you'll learn to create three types of organisms:

1. **A Simple Plant** - The foundation of the ecosystem
2. **A Herbivore** - An animal that eats plants
3. **A Carnivore** - A predator that hunts other animals

## Project Structure

Each creature is a separate .NET class library that references the OrganismBase assembly. Your project structure will look like:

```
MyCreature/
├── MyCreature.csproj
└── MyCreature.cs
```

## Creating Your First Project

### Step 1: Create a New Class Library

Create a new .NET class library project targeting `net10.0`:

```bash
dotnet new classlib -n MyFirstPlant -f net10.0
cd MyFirstPlant
```

### Step 2: Add OrganismBase Reference

Add a reference to the OrganismBase assembly:

```bash
dotnet add reference path/to/Terrarium.OrganismBase.csproj
```

Your `.csproj` file should look like:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Terrarium.OrganismBase\Terrarium.OrganismBase.csproj" />
  </ItemGroup>
</Project>
```

## Key Concepts

### Assembly Attributes

Every creature assembly requires two assembly-level attributes that identify your organism:

```csharp
[assembly: OrganismClass("Namespace.ClassName")]
[assembly: AuthorInformation("Your Name", "your.email@example.com")]
```

These attributes must match your organism's fully-qualified class name and provide author information.

### Base Classes

The OrganismBase API provides three base classes:

- `Organism` - Abstract base for all life forms (you won't use this directly)
- `Animal` - Base class for herbivores and carnivores
- `Plant` - Base class for stationary organisms

### Namespaces

The OrganismBase API uses the `OrganismBase` namespace (not `Terrarium.OrganismBase`):

```csharp
using OrganismBase;
```

### File-Scoped Namespaces (Modern C#)

We'll use file-scoped namespaces throughout these tutorials:

```csharp
namespace MyCreatures.MyFirstPlant;

public class MyFirstPlant : Plant
{
    // Your implementation
}
```

## Next Steps

Now that you understand the basics, let's create your first organism:

- [Tutorial 1: Creating a Simple Plant](./tutorial-1-simple-plant.md)
- [Tutorial 2: Creating a Herbivore](./tutorial-2-herbivore.md)
- [Tutorial 3: Creating a Carnivore](./tutorial-3-carnivore.md)

## Additional Resources

- [OrganismBase API Reference](../api/README.md)
- [Sample Creatures](../../../src/Terrarium.Samples/)
- [Architecture Documentation](../../architecture/)
