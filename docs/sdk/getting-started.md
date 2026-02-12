# Getting Started with Terrarium Creature Development

Welcome to .NET Terrarium! This guide will take you from zero to a deployed creature in **10 minutes**.

## What You'll Build

You'll create a simple herbivore beetle that:
- Wanders the terrain looking for plants
- Eats plants to gain energy
- Reproduces when healthy
- Survives in the ecosystem

## Prerequisites

- Visual Studio 2025 or Visual Studio Code with C# extension
- .NET 10 SDK ([download here](https://dotnet.microsoft.com/download))
- Terrarium running locally (see [main README](../../../README.md))
- 10 minutes of your time

## Step 1: Install the Template (30 seconds)

Open a terminal and install the Terrarium creature template:

```bash
dotnet new install Terrarium.Templates
```

You should see:
```
Success: Terrarium.Templates installed the following templates:
Template Name          Short Name            Language    Tags
---------------------  --------------------  ----------  -------------------------
Terrarium Creature     terrarium-creature    [C#]        Game/Terrarium/Creature
```

## Step 2: Create Your Creature Project (1 minute)

Create a new herbivore project:

```bash
dotnet new terrarium-creature -n MyBeetle --AuthorName "YourName" --AuthorEmail "you@example.com"
cd MyBeetle
```

This creates:
- `MyBeetle.csproj` - Project file with OrganismBase reference
- `MyBeetle.cs` - Your creature source code
- `README.md` - Quick reference

**Pro tip**: Use `--IsCarnivore true` to create a carnivore instead!

## Step 3: Understand the Code (2 minutes)

Open `MyBeetle.cs` in your editor. The template provides:

### Assembly Attributes (Identity)
```csharp
[assembly: OrganismClass("MyBeetle.MyBeetle")]
[assembly: AuthorInformation("YourName", "you@example.com")]
```
These identify your creature to the Terrarium engine.

### Characteristic Attributes (Stats)
```csharp
[MatureSize(26)]
[MaximumSpeedPoints(25)]
[CamouflagePoints(25)]
[EyesightPoints(50)]
```
Animals get **100 points** to distribute across characteristics. The template starts with a balanced build optimized for herbivores.

### Event-Driven Behavior
```csharp
protected override void Initialize()
{
    Idle += OnIdle;  // Called when creature has no action
    Load += OnLoad;  // Called at start of each turn
    Attacked += OnAttacked;  // Called when attacked
}
```

## Step 4: Customize the Behavior (3 minutes)

Let's make your beetle smarter! Replace the `OnIdle` method with this herbivore logic:

```csharp
private void OnIdle(object sender, IdleEventArgs e)
{
    try
    {
        // Reproduce when possible
        if (CanReproduce)
        {
            BeginReproduction(null);
            return;
        }

        // Scan for nearby organisms
        ArrayList organisms = Scan();
        
        // Look for plants to eat
        foreach (OrganismState organism in organisms)
        {
            if (organism is PlantState plant && plant.IsAlive)
            {
                // Move toward the plant
                if (!IsWithinRect(plant.GridX, plant.GridY, plant.CellRadius))
                {
                    BeginMoving(new MovementVector(
                        new Point(plant.Position.X, plant.Position.Y), 
                        2));
                    return;
                }
                
                // We're close enough - eat it!
                if (State == AnimalState.Idle)
                {
                    BeginEating(plant);
                    return;
                }
            }
        }

        // No plants found - wander randomly
        if (!IsMoving && State == AnimalState.Idle)
        {
            int randomX = OrganismRandom.Next(0, WorldWidth - 1);
            int randomY = OrganismRandom.Next(0, WorldHeight - 1);
            BeginMoving(new MovementVector(new Point(randomX, randomY), 2));
        }
    }
    catch (Exception ex)
    {
        WriteTrace($"Idle error: {ex.Message}");
    }
}
```

**What this does:**
1. Reproduces when energy is sufficient
2. Scans surroundings for plants
3. Moves toward and eats nearby plants
4. Wanders randomly when no food is found

## Step 5: Adjust Characteristics (1 minute)

For a food-finding herbivore, optimize the characteristic points:

```csharp
[MaximumEnergyPoints(10)]      // A bit more energy storage
[EatingSpeedPoints(20)]         // Eat plants faster
[AttackDamagePoints(0)]         // Herbivores don't attack
[DefendDamagePoints(10)]        // Basic defense
[MaximumSpeedPoints(20)]        // Good speed for escaping
[CamouflagePoints(15)]          // Moderate stealth
[EyesightPoints(25)]            // Lower since we use Scan()
```

**Remember:** Total must equal 100 points!

## Step 6: Build the DLL (30 seconds)

Build your creature:

```bash
dotnet build -c Release
```

Your creature DLL will be at: `bin/Release/net10.0/MyBeetle.dll`

## Step 7: Deploy to Terrarium (2 minutes)

### Option A: Via Web UI (Recommended)

1. Start Terrarium: `dotnet run --project src/Terrarium.Web`
2. Open browser: `http://localhost:5000`
3. Navigate to **Upload** page
4. Click **Browse Files** and select `bin/Release/net10.0/MyBeetle.dll`
5. Click **Upload**

The validator will verify:
- ✅ Inherits from Animal or Plant
- ✅ Has required assembly attributes
- ✅ No forbidden APIs (System.IO, System.Net, etc.)
- ✅ Characteristic points sum to 100

### Option B: Via File System

Copy your DLL to the PAC directory:
```bash
# Windows
copy bin\Release\net10.0\MyBeetle.dll %LOCALAPPDATA%\Terrarium\PAC\

# macOS/Linux  
cp bin/Release/net10.0/MyBeetle.dll ~/.terrarium/PAC/
```

## Step 8: Watch It Live! (30 seconds)

1. Navigate to the **Ecosystem** page
2. Your beetle will spawn automatically
3. Watch it explore, eat plants, and reproduce
4. Check the **Statistics** page to see your creature's population

**Success indicators:**
- 🟢 Your beetle appears in the organism list
- 🟢 Population increases over time
- 🟢 Energy levels fluctuate as it eats

## Troubleshooting

### Build Errors

**"Cannot find OrganismBase"**
- The template automatically references `Terrarium.OrganismBase` package
- If building from source, verify the package is available locally

**"Characteristic points don't sum to 100"**
- Count all the `*Points` attributes
- Must total exactly 100 for animals

### Upload Errors

**"Assembly attributes missing"**
- Verify `[assembly: OrganismClass(...)]` matches your fully-qualified class name
- Verify `[assembly: AuthorInformation(...)]` is present

**"Forbidden API usage"**
- Can't use: `System.IO`, `System.Net`, `System.Reflection`, threading, P/Invoke
- Stick to the OrganismBase API and basic .NET types

### Runtime Issues

**Creature dies immediately**
- Check your event handlers don't throw exceptions
- Use try-catch blocks around your logic
- Call `WriteTrace()` to debug

**Creature doesn't move**
- Verify you're calling `BeginMoving()` in the Idle handler
- Check movement isn't blocked by another action

## Next Steps

🎉 **Congratulations!** You've created and deployed your first Terrarium creature.

### Learn More

- **[Tutorial 1: Simple Plant](./tutorials/tutorial-1-simple-plant.md)** - Understand plant mechanics
- **[Tutorial 2: Herbivore Deep Dive](./tutorials/tutorial-2-herbivore.md)** - Advanced herbivore strategies
- **[Tutorial 3: Carnivore](./tutorials/tutorial-3-carnivore.md)** - Build a predator
- **[API Reference](./api/README.md)** - Complete OrganismBase documentation
- **[Sample Creatures](../../src/Terrarium.Samples/)** - Study proven implementations

### Ideas for Improvement

Try these challenges:

1. **Pack hunting** - Coordinate with your own species
2. **Flee from carnivores** - Detect and escape predators
3. **Territory control** - Defend a rich plant area
4. **Energy efficiency** - Minimize movement, maximize food
5. **Camouflage tactics** - Hide when energy is low

### Get Help

- **Discord**: Join our community (link in main README)
- **GitHub Issues**: Report bugs or ask questions
- **Documentation**: Check [docs/sdk/](./README.md) for detailed guides

## Quick Reference

### Useful Methods

```csharp
// Scanning
ArrayList organisms = Scan();  // Find nearby organisms

// Movement
BeginMoving(new MovementVector(destination, speed));
if (IsMoving) { /* currently moving */ }

// Eating
BeginEating(targetPlantOrCarrion);

// Combat (Carnivores only)
BeginAttacking(targetAnimal);
BeginDefending();

// Reproduction
if (CanReproduce)
    BeginReproduction(null);

// Debugging
WriteTrace("Debug message");
```

### State Properties

```csharp
State          // Current action (Idle, Moving, Eating, etc.)
EnergyState    // Current/max energy
IsAlive        // Still living
Position       // Current location
Direction      // Current heading (degrees)
```

### Characteristics to Tune

| Attribute | Effect |
|-----------|--------|
| `MatureSize` | Larger = more energy capacity, easier to see |
| `MaximumEnergyPoints` | Energy storage capacity |
| `EatingSpeedPoints` | How fast you consume food |
| `AttackDamagePoints` | Damage dealt when attacking |
| `DefendDamagePoints` | Damage reduction when defending |
| `MaximumSpeedPoints` | How fast you move |
| `CamouflagePoints` | Harder for predators to detect |
| `EyesightPoints` | Detection range for Scan() |

---

**Ready to dominate the ecosystem?** Start experimenting, share your creatures, and may the fittest code survive! 🦎🌿
