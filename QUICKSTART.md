# 🚀 Quick Start - Create Your First Terrarium Creature

**Time:** 10 minutes | **Difficulty:** Beginner | **Output:** A living creature in the ecosystem

## The Fast Path

```bash
# 1. Install template
dotnet new install Terrarium.Templates

# 2. Create herbivore
dotnet new terrarium-creature -n MyBeetle --AuthorName "YourName" --AuthorEmail "you@example.com"
cd MyBeetle

# 3. Build
dotnet build -c Release

# 4. Run Terrarium
dotnet run --project ../src/Terrarium.Web

# 5. Upload your creature
# → Open http://localhost:5000/upload
# → Select bin/Release/net10.0/MyBeetle.dll
# → Watch your creature live!
```

## What You Get

The template creates a fully functional creature with:
- ✅ Assembly attributes configured
- ✅ Characteristic points balanced for survival
- ✅ Event handlers wired up
- ✅ Sample behavior (wander + reproduce)
- ✅ Ready to build and deploy

## Customize It

Open `MyBeetle.cs` and:
1. Adjust characteristic points (100 total for animals)
2. Modify the `OnIdle()` behavior
3. Add hunting, fleeing, or pack tactics

## Full Tutorial

For step-by-step instructions with explanations:

**📖 [Complete Getting Started Guide](./docs/sdk/getting-started.md)**

Includes:
- Detailed code walkthrough
- Smart herbivore behavior example
- Troubleshooting tips
- Ideas for improvement
- Quick reference

## Other Templates

```bash
# Carnivore (hunts other animals)
dotnet new terrarium-creature -n MyCarnivore --IsCarnivore true

# Plant (stationary, spreads seeds)
dotnet new terrarium-creature -n MyPlant --CreatureType Plant
```

## Learn More

- **[SDK Documentation](./docs/sdk/)** - Full API reference and tutorials
- **[Sample Creatures](./src/Terrarium.Samples/)** - Working examples to study
- **[Architecture Docs](./docs/architecture/)** - How the engine works

## Need Help?

- Check [docs/sdk/getting-started.md](./docs/sdk/getting-started.md) for troubleshooting
- Review [docs/sdk/api/](./docs/sdk/api/) for API details
- Study sample creatures in [src/Terrarium.Samples/](./src/Terrarium.Samples/)

---

**May the fittest code survive!** 🦎🌿
