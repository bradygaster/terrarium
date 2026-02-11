// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Drawing;
using System.Reflection;
using System.Text;
using OrganismBase;

namespace Terrarium.Game;

/// <summary>
/// Base class for animal and plant species. Contains characteristics
/// and abilities read from CLR attributes on the creature's Type.
/// </summary>
public abstract class Species : ISpecies
{
    private readonly string _assemblyFullName;
    private readonly int _maximumEnergyPerUnitRadius;
    private readonly string _typeName;
    private Type? _speciesType;

    protected Species(Type clrType)
    {
        MarkingColor = KnownColor.Black;
        _typeName = clrType.AssemblyQualifiedName!;
        _assemblyFullName = clrType.Assembly.FullName!;

        var energyAttr = (MaximumEnergyPointsAttribute?)Attribute.GetCustomAttribute(clrType, typeof(MaximumEnergyPointsAttribute));
        if (energyAttr == null) throw new GameEngineException("MaximumEnergyPointsAttribute is required.");
        _maximumEnergyPerUnitRadius = energyAttr.MaximumEnergyPerUnitRadius;

        var matureAttr = (MatureSizeAttribute?)Attribute.GetCustomAttribute(clrType, typeof(MatureSizeAttribute));
        if (matureAttr == null) throw new GameEngineException("MatureSizeAttribute is required.");
        MatureRadius = matureAttr.MatureRadius;

        var markingAttr = (MarkingColorAttribute?)Attribute.GetCustomAttribute(clrType, typeof(MarkingColorAttribute));
        if (markingAttr != null) MarkingColor = markingAttr.MarkingColor;

        var authInfo = (AuthorInformationAttribute?)Attribute.GetCustomAttribute(clrType.Assembly, typeof(AuthorInformationAttribute));
        if (authInfo == null || string.IsNullOrEmpty(authInfo.AuthorName))
            throw new GameEngineException("AuthorInformationAttribute is required.");
        AuthorName = authInfo.AuthorName;
        AuthorEmail = authInfo.AuthorEmail;
        Name = GetAssemblyShortName(clrType.Assembly.FullName!);
    }

    internal Type Type
    {
        get
        {
            if (_speciesType != null) return _speciesType;
            try { _speciesType = Type.GetType(_typeName); } catch { }
            _speciesType ??= typeof(Organism);
            return _speciesType;
        }
    }

    public string AuthorName { get; private set; }
    public string AuthorEmail { get; private set; }
    public KnownColor MarkingColor { get; private set; }
    public int InitialRadius => (EngineSettings.MinMatureSize / 2) - 1;
    public string Name { get; private set; }
    public string AssemblyFullName => _assemblyFullName;
    public int MatureRadius { get; private set; }
    public string Skin { get; protected set; } = string.Empty;
    public abstract int ReproductionWait { get; }
    public abstract int LifeSpan { get; }
    public int GrowthWait => (LifeSpan / 2) / (MatureRadius - InitialRadius);
    public int MaximumEnergyPerUnitRadius => _maximumEnergyPerUnitRadius;

    public bool IsSameSpecies(ISpecies species) => ((Species)species).Type == Type;

    public virtual string GetAttributeWarnings()
    {
        var warnings = new StringBuilder();
        var ea = (MaximumEnergyPointsAttribute?)Attribute.GetCustomAttribute(Type, typeof(MaximumEnergyPointsAttribute));
        var w = ea?.GetWarnings() ?? "";
        if (w.Length != 0) { warnings.Append(w); warnings.Append(Environment.NewLine); }
        return warnings.ToString();
    }

    public abstract OrganismState InitializeNewState(Point position, int generation);

    public static Species GetSpeciesFromAssembly(Assembly organismAssembly)
    {
        var hasAttr = false;
        var attributes = Attribute.GetCustomAttributes(organismAssembly);
        if (attributes.Length == 0) throw new GameEngineException("OrganismClassAttribute is required.");
        foreach (var a in attributes) { if (a.GetType().Name == "OrganismClassAttribute") { hasAttr = true; break; } }

        var classAttr = (OrganismClassAttribute?)Attribute.GetCustomAttribute(organismAssembly, typeof(OrganismClassAttribute));
        if (classAttr == null)
        {
            if (hasAttr) throw new GameEngineException("Your organism is built against a different version of Terrarium. Try rebuilding it.");
            throw new GameEngineException("OrganismClassAttribute is required.");
        }

        Type clrType;
        try { clrType = organismAssembly.GetType(classAttr.ClassName, true)!; }
        catch (TypeLoadException) { throw new GameEngineException($"Your organism {classAttr.ClassName} could not be found in the assembly."); }

        if (typeof(Plant).IsAssignableFrom(clrType)) return new PlantSpecies(clrType);
        if (typeof(Animal).IsAssignableFrom(clrType)) return new AnimalSpecies(clrType);
        throw new GameEngineException($"Class specified in OrganismClassAttribute ({classAttr.ClassName}) doesn't derive from Animal or Plant");
    }

    internal static string GetAssemblyShortName(string fullName)
    {
        var idx = fullName.IndexOf(',');
        return idx > 0 ? fullName[..idx] : fullName;
    }
}
