// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Diagnostics;
using System.Drawing;
using System.Text;
using OrganismBase;

namespace Terrarium.Game;

/// <summary>
/// Holds all species information about an animal.
/// </summary>
public sealed class AnimalSpecies : Species, IAnimalSpecies
{
    private readonly int _eatingSpeedPerUnitRadius;

    public AnimalSpecies(Type clrType) : base(clrType)
    {
        SkinFamily = AnimalSkinFamily.Spider;
        var totalPoints = 0;
        Debug.Assert(clrType != null);
        Debug.Assert(typeof(Animal).IsAssignableFrom(clrType));

        var skinAttr = (AnimalSkinAttribute?)Attribute.GetCustomAttribute(clrType, typeof(AnimalSkinAttribute));
        if (skinAttr != null) { SkinFamily = skinAttr.SkinFamily; Skin = skinAttr.Skin; }

        var carnAttr = (CarnivoreAttribute?)Attribute.GetCustomAttribute(clrType, typeof(CarnivoreAttribute));
        if (carnAttr == null) throw new GameEngineException("CarnivoreAttribute is required.");
        IsCarnivore = carnAttr.IsCarnivore;

        var eatAttr = (EatingSpeedPointsAttribute?)Attribute.GetCustomAttribute(clrType, typeof(EatingSpeedPointsAttribute));
        if (eatAttr == null) throw new GameEngineException("EatingSpeedPointsAttribute is required.");
        _eatingSpeedPerUnitRadius = eatAttr.EatingSpeedPerUnitRadius;
        totalPoints += eatAttr.Points;

        var atkAttr = (AttackDamagePointsAttribute?)Attribute.GetCustomAttribute(clrType, typeof(AttackDamagePointsAttribute));
        if (atkAttr == null) throw new GameEngineException("AttackDamagePointsAttribute is required.");
        MaximumAttackDamagePerUnitRadius = IsCarnivore
            ? (int)(atkAttr.MaximumAttackDamagePerUnitRadius * EngineSettings.CarnivoreAttackDefendMultiplier)
            : atkAttr.MaximumAttackDamagePerUnitRadius;
        totalPoints += atkAttr.Points;

        var defAttr = (DefendDamagePointsAttribute?)Attribute.GetCustomAttribute(clrType, typeof(DefendDamagePointsAttribute));
        if (defAttr == null) throw new GameEngineException("DefendDamagePointsAttribute is required.");
        MaximumDefendDamagePerUnitRadius = IsCarnivore
            ? (int)(defAttr.MaximumDefendDamagePerUnitRadius * EngineSettings.CarnivoreAttackDefendMultiplier)
            : defAttr.MaximumDefendDamagePerUnitRadius;
        totalPoints += defAttr.Points;

        var nrgAttr = (MaximumEnergyPointsAttribute?)Attribute.GetCustomAttribute(clrType, typeof(MaximumEnergyPointsAttribute));
        if (nrgAttr == null) throw new GameEngineException("MaximumEnergyPointsAttribute is required.");
        totalPoints += nrgAttr.Points;

        var spdAttr = (MaximumSpeedPointsAttribute?)Attribute.GetCustomAttribute(clrType, typeof(MaximumSpeedPointsAttribute));
        if (spdAttr == null) throw new GameEngineException("MaximumSpeedPointsAttribute is required.");
        totalPoints += spdAttr.Points;
        MaximumSpeed = spdAttr.MaximumSpeed;

        var camAttr = (CamouflagePointsAttribute?)Attribute.GetCustomAttribute(clrType, typeof(CamouflagePointsAttribute));
        if (camAttr == null) throw new GameEngineException("CamouflagePointsAttribute is required.");
        totalPoints += camAttr.Points;
        InvisibleOdds = camAttr.InvisibleOdds;

        var eyeAttr = (EyesightPointsAttribute?)Attribute.GetCustomAttribute(clrType, typeof(EyesightPointsAttribute));
        if (eyeAttr == null) throw new GameEngineException("EyesightPointsAttribute is required.");
        totalPoints += eyeAttr.Points;
        EyesightRadius = eyeAttr.EyesightRadius;

        if (totalPoints > EngineSettings.MaxAvailableCharacteristicPoints) throw new TooManyPointsException();
    }

    public override int ReproductionWait => MatureRadius * EngineSettings.AnimalReproductionWaitPerUnitRadius;
    public override int LifeSpan => IsCarnivore
        ? MatureRadius * EngineSettings.AnimalLifeSpanPerUnitMaximumRadius * EngineSettings.CarnivoreLifeSpanMultiplier
        : MatureRadius * EngineSettings.AnimalLifeSpanPerUnitMaximumRadius;

    public bool IsCarnivore { get; private set; }
    public int EatingSpeedPerUnitRadius => _eatingSpeedPerUnitRadius;
    public AnimalSkinFamily SkinFamily { get; private set; }
    public int MaximumAttackDamagePerUnitRadius { get; private set; }
    public int MaximumDefendDamagePerUnitRadius { get; private set; }
    public int MaximumSpeed { get; private set; }
    public int InvisibleOdds { get; private set; }
    public int EyesightRadius { get; private set; }

    public override string GetAttributeWarnings()
    {
        var w = new StringBuilder();
        w.Append(base.GetAttributeWarnings());
        AppendWarning(w, Attribute.GetCustomAttribute(Type, typeof(EatingSpeedPointsAttribute)) as PointBasedCharacteristicAttribute);
        AppendWarning(w, Attribute.GetCustomAttribute(Type, typeof(AttackDamagePointsAttribute)) as PointBasedCharacteristicAttribute);
        AppendWarning(w, Attribute.GetCustomAttribute(Type, typeof(DefendDamagePointsAttribute)) as PointBasedCharacteristicAttribute);
        AppendWarning(w, Attribute.GetCustomAttribute(Type, typeof(MaximumEnergyPointsAttribute)) as PointBasedCharacteristicAttribute);
        AppendWarning(w, Attribute.GetCustomAttribute(Type, typeof(MaximumSpeedPointsAttribute)) as PointBasedCharacteristicAttribute);
        AppendWarning(w, Attribute.GetCustomAttribute(Type, typeof(CamouflagePointsAttribute)) as PointBasedCharacteristicAttribute);
        AppendWarning(w, Attribute.GetCustomAttribute(Type, typeof(EyesightPointsAttribute)) as PointBasedCharacteristicAttribute);
        return w.ToString();
    }

    private static void AppendWarning(StringBuilder sb, PointBasedCharacteristicAttribute? attr)
    {
        if (attr == null) return;
        var w = attr.GetWarnings();
        if (w.Length != 0) { sb.Append(w); sb.Append(Environment.NewLine); }
    }

    public override OrganismState InitializeNewState(Point position, int generation)
    {
        var newState = new AnimalState(Guid.NewGuid().ToString(), this, generation, EnergyState.Hungry, InitialRadius)
            { Position = position };
        newState.ResetGrowthWait();
        return newState;
    }
}
