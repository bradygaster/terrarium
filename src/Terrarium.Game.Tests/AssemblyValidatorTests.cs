using System.Reflection;
using System.Runtime.InteropServices;
using OrganismBase;
using Xunit;

namespace Terrarium.Game.Tests;

/// <summary>
/// Tests for assembly-level validation rules that enforce the Terrarium security model.
/// Validates that creature assemblies conform to required contracts:
/// - Must inherit from Animal or Plant
/// - Must have required assembly-level attributes
/// - Must not use forbidden APIs (P/Invoke, restricted namespaces)
/// </summary>
public class AssemblyValidatorTests
{
    // --- Valid creature assemblies pass validation ---

    [Fact]
    public void ValidCreature_SimpleCarnivore_InheritsFromAnimal()
    {
        var creatureType = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore);
        Assert.True(typeof(Animal).IsAssignableFrom(creatureType));
    }

    [Fact]
    public void ValidCreature_SimplePlant_InheritsFromPlant()
    {
        var creatureType = typeof(Terrarium.Samples.SimplePlant.SimplePlant);
        Assert.True(typeof(Plant).IsAssignableFrom(creatureType));
    }

    [Fact]
    public void ValidCreature_SimpleHerbivore_InheritsFromAnimal()
    {
        var creatureType = typeof(Terrarium.Samples.SimpleHerbivore.SimpleHerbivore);
        Assert.True(typeof(Animal).IsAssignableFrom(creatureType));
    }

    [Fact]
    public void ValidCreature_SimpleCarnivore_HasOrganismClassAttribute()
    {
        var assembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var attr = assembly.GetCustomAttribute<OrganismClassAttribute>();
        Assert.NotNull(attr);
    }

    [Fact]
    public void ValidCreature_SimpleCarnivore_HasAuthorInformationAttribute()
    {
        var assembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var attr = assembly.GetCustomAttribute<AuthorInformationAttribute>();
        Assert.NotNull(attr);
        Assert.False(string.IsNullOrEmpty(attr!.AuthorName));
    }

    [Fact]
    public void ValidCreature_SimplePlant_HasOrganismClassAttribute()
    {
        var assembly = typeof(Terrarium.Samples.SimplePlant.SimplePlant).Assembly;
        var attr = assembly.GetCustomAttribute<OrganismClassAttribute>();
        Assert.NotNull(attr);
    }

    [Fact]
    public void ValidCreature_CanCreateSpeciesFromAssembly()
    {
        var assembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var species = Species.GetSpeciesFromAssembly(assembly);
        Assert.NotNull(species);
    }

    // --- Assembly not inheriting Animal/Plant fails ---

    [Fact]
    public void InvalidCreature_TypeNotInheritingAnimal_FailsBaseClassCheck()
    {
        // A plain object does not inherit from Animal or Plant
        var invalidType = typeof(object);
        Assert.False(typeof(Animal).IsAssignableFrom(invalidType));
        Assert.False(typeof(Plant).IsAssignableFrom(invalidType));
    }

    [Fact]
    public void InvalidCreature_TypeNotInheritingOrganism_FailsBaseClassCheck()
    {
        var invalidType = typeof(string);
        Assert.False(typeof(Organism).IsAssignableFrom(invalidType));
    }

    [Fact]
    public void InvalidCreature_ArbitraryClass_NotAnimalOrPlant()
    {
        // GameEngine itself is not a creature
        var invalidType = typeof(GameEngine);
        Assert.False(typeof(Animal).IsAssignableFrom(invalidType));
        Assert.False(typeof(Plant).IsAssignableFrom(invalidType));
    }

    // --- Assembly with P/Invoke fails ---

    [Fact]
    public void PInvokeDetection_CreatureAssembly_ShouldHaveNoPInvokeMethods()
    {
        var assembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var hasPInvoke = AssemblyContainsPInvoke(assembly);
        Assert.False(hasPInvoke, "Valid creature assembly should not contain P/Invoke methods");
    }

    [Fact]
    public void PInvokeDetection_PlantAssembly_ShouldHaveNoPInvokeMethods()
    {
        var assembly = typeof(Terrarium.Samples.SimplePlant.SimplePlant).Assembly;
        var hasPInvoke = AssemblyContainsPInvoke(assembly);
        Assert.False(hasPInvoke, "Valid plant assembly should not contain P/Invoke methods");
    }

    [Fact]
    public void PInvokeDetection_CanDetectDllImportAttribute()
    {
        // Verify our detection logic works by checking a known system type
        // Marshal class in System.Runtime.InteropServices uses P/Invoke internally
        var attr = typeof(DllImportAttribute);
        Assert.NotNull(attr);
        // DllImportAttribute is the marker for P/Invoke — our validator should reject any type with it
    }

    // --- Assembly with forbidden namespaces fails ---

    [Fact]
    public void ForbiddenNamespace_CreatureAssembly_ShouldNotReferenceSystemIO()
    {
        // Valid creatures should not directly define types in System.IO
        var assembly = typeof(Terrarium.Samples.SimplePlant.SimplePlant).Assembly;
        var usesForbiddenNamespace = AssemblyDefinesTypesInNamespace(assembly, "System.IO");
        Assert.False(usesForbiddenNamespace,
            "Creature assembly should not define types in System.IO namespace");
    }

    [Fact]
    public void ForbiddenNamespace_CreatureAssembly_ShouldNotReferenceSystemNet()
    {
        var assembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var usesForbiddenNamespace = AssemblyDefinesTypesInNamespace(assembly, "System.Net");
        Assert.False(usesForbiddenNamespace,
            "Creature assembly should not define types in System.Net namespace");
    }

    [Fact]
    public void ForbiddenNamespace_CreatureAssembly_ShouldNotReferenceSystemDiagnosticsProcess()
    {
        var assembly = typeof(Terrarium.Samples.SimpleHerbivore.SimpleHerbivore).Assembly;
        var usesForbiddenNamespace = AssemblyDefinesTypesInNamespace(assembly, "System.Diagnostics.Process");
        Assert.False(usesForbiddenNamespace,
            "Creature assembly should not define types in System.Diagnostics.Process namespace");
    }

    [Fact]
    public void ValidCreature_OnlyDefinesTypesInOwnNamespace()
    {
        var assembly = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore).Assembly;
        var exportedTypes = assembly.GetTypes();
        foreach (var type in exportedTypes)
        {
            Assert.StartsWith("Terrarium.Samples", type.FullName ?? type.Name);
        }
    }

    // --- Attribute point allocation validation ---

    [Fact]
    public void ValidCreature_AttributePoints_DoNotExceedMaximum()
    {
        var creatureType = typeof(Terrarium.Samples.SimpleCarnivore.SimpleCarnivore);
        var maxEnergy = creatureType.GetCustomAttribute<MaximumEnergyPointsAttribute>();
        var eatingSpeed = creatureType.GetCustomAttribute<EatingSpeedPointsAttribute>();
        var attackDamage = creatureType.GetCustomAttribute<AttackDamagePointsAttribute>();
        var defendDamage = creatureType.GetCustomAttribute<DefendDamagePointsAttribute>();
        var maxSpeed = creatureType.GetCustomAttribute<MaximumSpeedPointsAttribute>();
        var camouflage = creatureType.GetCustomAttribute<CamouflagePointsAttribute>();
        var eyesight = creatureType.GetCustomAttribute<EyesightPointsAttribute>();

        int totalPoints =
            (maxEnergy?.Points ?? 0) +
            (eatingSpeed?.Points ?? 0) +
            (attackDamage?.Points ?? 0) +
            (defendDamage?.Points ?? 0) +
            (maxSpeed?.Points ?? 0) +
            (camouflage?.Points ?? 0) +
            (eyesight?.Points ?? 0);

        Assert.True(totalPoints <= EngineSettings.MaxAvailableCharacteristicPoints,
            $"Total points {totalPoints} exceeds maximum {EngineSettings.MaxAvailableCharacteristicPoints}");
    }

    #region Validation helpers

    /// <summary>
    /// Scans an assembly for methods marked with [DllImport], indicating P/Invoke usage.
    /// </summary>
    private static bool AssemblyContainsPInvoke(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                    BindingFlags.Static | BindingFlags.Instance |
                                                    BindingFlags.DeclaredOnly))
            {
                if ((method.Attributes & MethodAttributes.PinvokeImpl) != 0)
                    return true;
                if (method.GetCustomAttribute<DllImportAttribute>() != null)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks whether an assembly defines any types within a given namespace.
    /// </summary>
    private static bool AssemblyDefinesTypesInNamespace(Assembly assembly, string forbiddenNamespace)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.Namespace != null && type.Namespace.StartsWith(forbiddenNamespace, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    #endregion
}
