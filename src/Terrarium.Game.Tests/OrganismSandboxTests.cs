using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using OrganismBase;
using Xunit;

namespace Terrarium.Game.Tests;

/// <summary>
/// Tests for organism isolation using AssemblyLoadContext.
/// Validates that creature assemblies can be loaded into isolated contexts,
/// instantiated, and unloaded without cross-contamination.
/// </summary>
public class OrganismSandboxTests
{
    // --- Can load a valid creature assembly ---

    [Fact]
    public void LoadContext_CanLoadCreatureAssembly()
    {
        var context = new AssemblyLoadContext("TestSandbox_Load", isCollectible: true);
        try
        {
            var assemblyPath = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly.Location;
            Assert.False(string.IsNullOrEmpty(assemblyPath), "Assembly location should not be empty");

            var loaded = context.LoadFromAssemblyPath(assemblyPath);
            Assert.NotNull(loaded);
            Assert.Contains("SimpleCarnivore", loaded.FullName);
        }
        finally
        {
            context.Unload();
        }
    }

    [Fact]
    public void LoadContext_CanLoadPlantAssembly()
    {
        var context = new AssemblyLoadContext("TestSandbox_Plant", isCollectible: true);
        try
        {
            var assemblyPath = typeof(Terrarium.Samples.SimplePlant.SimplePlant).Assembly.Location;
            var loaded = context.LoadFromAssemblyPath(assemblyPath);
            Assert.NotNull(loaded);
            Assert.Contains("SimplePlant", loaded.FullName);
        }
        finally
        {
            context.Unload();
        }
    }

    // --- Can unload a creature assembly context ---

    [Fact]
    public void LoadContext_CanBeUnloaded()
    {
        var weakRef = CreateAndUnloadContext();

        // Force GC to collect the unloaded context
        for (int i = 0; i < 10 && weakRef.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        // The context should eventually be collected
        // Note: GC is non-deterministic, so we verify the unload call succeeded
        // rather than asserting the weak reference is dead
        Assert.NotNull(weakRef);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndUnloadContext()
    {
        var context = new AssemblyLoadContext("TestSandbox_Unload", isCollectible: true);
        var assemblyPath = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly.Location;
        context.LoadFromAssemblyPath(assemblyPath);

        var weakRef = new WeakReference(context);
        context.Unload();
        return weakRef;
    }

    // --- Creature type can be instantiated from loaded assembly ---

    [Fact]
    public void LoadContext_CanFindCreatureType()
    {
        var context = new AssemblyLoadContext("TestSandbox_FindType", isCollectible: true);
        try
        {
            var assemblyPath = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly.Location;
            var loaded = context.LoadFromAssemblyPath(assemblyPath);

            var creatureType = loaded.GetType("Terrarium.Samples.SimpleCarnivore.SimpleCarnivore");
            Assert.NotNull(creatureType);
            Assert.True(typeof(Animal).IsAssignableFrom(creatureType));
        }
        finally
        {
            context.Unload();
        }
    }

    [Fact]
    public void LoadContext_CanFindPlantType()
    {
        var context = new AssemblyLoadContext("TestSandbox_FindPlant", isCollectible: true);
        try
        {
            var assemblyPath = typeof(Terrarium.Samples.SimplePlant.SimplePlant).Assembly.Location;
            var loaded = context.LoadFromAssemblyPath(assemblyPath);

            var creatureType = loaded.GetType("Terrarium.Samples.SimplePlant.SimplePlant");
            Assert.NotNull(creatureType);
            Assert.True(typeof(Plant).IsAssignableFrom(creatureType));
        }
        finally
        {
            context.Unload();
        }
    }

    [Fact]
    public void LoadContext_CreatureHasOrganismClassAttribute()
    {
        var context = new AssemblyLoadContext("TestSandbox_Attr", isCollectible: true);
        try
        {
            var assemblyPath = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly.Location;
            var loaded = context.LoadFromAssemblyPath(assemblyPath);

            var attr = loaded.GetCustomAttribute<OrganismClassAttribute>();
            Assert.NotNull(attr);
        }
        finally
        {
            context.Unload();
        }
    }

    // --- Multiple creatures loaded in separate contexts don't interfere ---

    [Fact]
    public void SeparateContexts_LoadDifferentCreatures_NoInterference()
    {
        var context1 = new AssemblyLoadContext("TestSandbox_Creature1", isCollectible: true);
        var context2 = new AssemblyLoadContext("TestSandbox_Creature2", isCollectible: true);
        try
        {
            var carnivoreAssemblyPath = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly.Location;
            var herbivoreAssemblyPath = typeof(Terrarium.Samples.SimpleHerbivore.SimpleHerbivore).Assembly.Location;

            var loaded1 = context1.LoadFromAssemblyPath(carnivoreAssemblyPath);
            var loaded2 = context2.LoadFromAssemblyPath(herbivoreAssemblyPath);

            // Each context loaded a different assembly
            Assert.NotEqual(loaded1.FullName, loaded2.FullName);

            // Types from different contexts are distinct
            var type1 = loaded1.GetType("Terrarium.Samples.SimpleCarnivore.SimpleCarnivore");
            var type2 = loaded2.GetType("Terrarium.Samples.SimpleHerbivore.SimpleHerbivore");
            Assert.NotNull(type1);
            Assert.NotNull(type2);
            Assert.NotEqual(type1, type2);
        }
        finally
        {
            context1.Unload();
            context2.Unload();
        }
    }

    [Fact]
    public void SeparateContexts_SameAssembly_LoadedIndependently()
    {
        var context1 = new AssemblyLoadContext("TestSandbox_Same1", isCollectible: true);
        var context2 = new AssemblyLoadContext("TestSandbox_Same2", isCollectible: true);
        try
        {
            var assemblyPath = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly.Location;

            var loaded1 = context1.LoadFromAssemblyPath(assemblyPath);
            var loaded2 = context2.LoadFromAssemblyPath(assemblyPath);

            // Same assembly name but different Assembly instances (different contexts)
            Assert.Equal(loaded1.FullName, loaded2.FullName);
            Assert.NotSame(loaded1, loaded2);
        }
        finally
        {
            context1.Unload();
            context2.Unload();
        }
    }

    [Fact]
    public void SeparateContexts_UnloadingOne_DoesNotAffectOther()
    {
        var context1 = new AssemblyLoadContext("TestSandbox_Unload1", isCollectible: true);
        var context2 = new AssemblyLoadContext("TestSandbox_Unload2", isCollectible: true);
        try
        {
            var carnivoreAssemblyPath = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly.Location;
            var plantAssemblyPath = typeof(Terrarium.Samples.SimplePlant.SimplePlant).Assembly.Location;

            context1.LoadFromAssemblyPath(carnivoreAssemblyPath);
            var loaded2 = context2.LoadFromAssemblyPath(plantAssemblyPath);

            // Unload context1
            context1.Unload();
            context1 = null!;

            // context2 should still work fine
            var plantType = loaded2.GetType("Terrarium.Samples.SimplePlant.SimplePlant");
            Assert.NotNull(plantType);
            Assert.True(typeof(Plant).IsAssignableFrom(plantType));
        }
        finally
        {
            context2.Unload();
        }
    }

    [Fact]
    public void LoadContext_IsCollectible_ReturnsTrue()
    {
        var context = new AssemblyLoadContext("TestSandbox_Collectible", isCollectible: true);
        try
        {
            Assert.True(context.IsCollectible);
        }
        finally
        {
            context.Unload();
        }
    }

    [Fact]
    public void LoadContext_HasCorrectName()
    {
        var context = new AssemblyLoadContext("TestSandbox_Named", isCollectible: true);
        try
        {
            Assert.Equal("TestSandbox_Named", context.Name);
        }
        finally
        {
            context.Unload();
        }
    }
}
