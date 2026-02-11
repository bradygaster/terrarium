using OrganismBase;
using Xunit;

namespace Terrarium.OrganismBase.Tests;

public class PointBasedAttributeTests
{
    // --- AttackDamagePointsAttribute ---

    [Fact]
    public void AttackDamage_ZeroPoints_ReturnsBaseValue()
    {
        var attr = new AttackDamagePointsAttribute(0);
        Assert.Equal(0, attr.Points);
        Assert.Equal(EngineSettings.BaseInflictedDamagePerUnitOfRadius, attr.MaximumAttackDamagePerUnitRadius);
    }

    [Fact]
    public void AttackDamage_MaxPoints_ReturnsBaseAndMaxValue()
    {
        var attr = new AttackDamagePointsAttribute(100);
        Assert.Equal(100, attr.Points);
        var expected = (int)((EngineSettings.BaseInflictedDamagePerUnitOfRadius +
            1.0f * EngineSettings.MaximumInflictedDamagePerUnitOfRadius) + 0.001f);
        Assert.Equal(expected, attr.MaximumAttackDamagePerUnitRadius);
    }

    [Fact]
    public void AttackDamage_MidPoints_ReturnsInterpolatedValue()
    {
        var attr = new AttackDamagePointsAttribute(50);
        Assert.Equal(50, attr.Points);
        var expected = (int)((EngineSettings.BaseInflictedDamagePerUnitOfRadius +
            0.5f * EngineSettings.MaximumInflictedDamagePerUnitOfRadius) + 0.001f);
        Assert.Equal(expected, attr.MaximumAttackDamagePerUnitRadius);
    }

    [Fact]
    public void AttackDamage_NegativePoints_Throws()
    {
        Assert.Throws<TooManyPointsOnOneCharacteristicException>(() => new AttackDamagePointsAttribute(-1));
    }

    [Fact]
    public void AttackDamage_OverMaxPoints_Throws()
    {
        Assert.Throws<TooManyPointsOnOneCharacteristicException>(() => new AttackDamagePointsAttribute(101));
    }

    // --- DefendDamagePointsAttribute ---

    [Fact]
    public void DefendDamage_ZeroPoints_ReturnsBaseValue()
    {
        var attr = new DefendDamagePointsAttribute(0);
        Assert.Equal(EngineSettings.BaseDefendedDamagePerUnitOfRadius, attr.MaximumDefendDamagePerUnitRadius);
    }

    [Fact]
    public void DefendDamage_MaxPoints_ReturnsBaseAndMaxValue()
    {
        var attr = new DefendDamagePointsAttribute(100);
        var expected = (int)((EngineSettings.BaseDefendedDamagePerUnitOfRadius +
            1.0f * EngineSettings.MaximumDefendedDamagePerUnitOfRadius) + 0.001f);
        Assert.Equal(expected, attr.MaximumDefendDamagePerUnitRadius);
    }

    // --- EatingSpeedPointsAttribute ---

    [Fact]
    public void EatingSpeed_ZeroPoints_ReturnsBaseValue()
    {
        var attr = new EatingSpeedPointsAttribute(0);
        Assert.Equal(EngineSettings.BaseEatingSpeedPerUnitOfRadius, attr.EatingSpeedPerUnitRadius);
    }

    [Fact]
    public void EatingSpeed_MaxPoints_ReturnsMaxValue()
    {
        var attr = new EatingSpeedPointsAttribute(100);
        var expected = (int)(EngineSettings.BaseEatingSpeedPerUnitOfRadius +
            1.0f * EngineSettings.MaximumEatingSpeedPerUnitOfRadius);
        Assert.Equal(expected, attr.EatingSpeedPerUnitRadius);
    }

    // --- EyesightPointsAttribute ---

    [Fact]
    public void Eyesight_ZeroPoints_ReturnsBaseValue()
    {
        var attr = new EyesightPointsAttribute(0);
        Assert.Equal(EngineSettings.BaseEyesightRadius, attr.EyesightRadius);
    }

    [Fact]
    public void Eyesight_MaxPoints_ReturnsMaxValue()
    {
        var attr = new EyesightPointsAttribute(100);
        var expected = (int)(EngineSettings.BaseEyesightRadius +
            1.0f * EngineSettings.MaximumEyesightRadius);
        Assert.Equal(expected, attr.EyesightRadius);
    }

    // --- MaximumSpeedPointsAttribute ---

    [Fact]
    public void MaxSpeed_ZeroPoints_ReturnsBaseValue()
    {
        var attr = new MaximumSpeedPointsAttribute(0);
        Assert.Equal(EngineSettings.SpeedBase, attr.MaximumSpeed);
    }

    [Fact]
    public void MaxSpeed_MaxPoints_ReturnsMaxValue()
    {
        var attr = new MaximumSpeedPointsAttribute(100);
        var expected = (int)(EngineSettings.SpeedBase + 1.0f * EngineSettings.SpeedMaximum);
        Assert.Equal(expected, attr.MaximumSpeed);
    }

    // --- MaximumEnergyPointsAttribute ---

    [Fact]
    public void MaxEnergy_ZeroPoints_ReturnsBaseValue()
    {
        var attr = new MaximumEnergyPointsAttribute(0);
        Assert.Equal((int)(float)EngineSettings.MaxEnergyBasePerUnitRadius, attr.MaximumEnergyPerUnitRadius);
    }

    [Fact]
    public void MaxEnergy_MaxPoints_ReturnsMaxValue()
    {
        var attr = new MaximumEnergyPointsAttribute(100);
        var expected = (int)((float)EngineSettings.MaxEnergyBasePerUnitRadius +
            1.0f * (float)EngineSettings.MaxEnergyMaximumPerUnitRadius);
        Assert.Equal(expected, attr.MaximumEnergyPerUnitRadius);
    }

    // --- CamouflagePointsAttribute ---

    [Fact]
    public void Camouflage_ZeroPoints_ReturnsBaseValue()
    {
        var attr = new CamouflagePointsAttribute(0);
        Assert.Equal(EngineSettings.InvisibleOddsBase, attr.InvisibleOdds);
    }

    [Fact]
    public void Camouflage_MaxPoints_ReturnsMaxValue()
    {
        var attr = new CamouflagePointsAttribute(100);
        var expected = (int)(EngineSettings.InvisibleOddsBase + 1.0f * EngineSettings.InvisibleOddsMaximum);
        Assert.Equal(expected, attr.InvisibleOdds);
    }

    // --- GetWarnings ---

    [Fact]
    public void GetWarnings_AlignedPoints_ReturnsEmpty()
    {
        // MaximumInflictedDamagePerUnitOfRadius is 25, so increment = 100/25 = 4
        var attr = new AttackDamagePointsAttribute(8);
        Assert.Equal(string.Empty, attr.GetWarnings());
    }

    [Fact]
    public void GetWarnings_UnalignedPoints_ReturnsWarning()
    {
        // MaximumInflictedDamagePerUnitOfRadius is 25, so increment = 100/25 = 4
        var attr = new AttackDamagePointsAttribute(3);
        var warning = attr.GetWarnings();
        Assert.NotEmpty(warning);
        Assert.Contains("increments of", warning);
    }
}
