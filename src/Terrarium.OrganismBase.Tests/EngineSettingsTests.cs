using OrganismBase;
using Xunit;

namespace Terrarium.OrganismBase.Tests;

public class EngineSettingsTests
{
    [Fact]
    public void MaxAvailableCharacteristicPoints_Is100()
    {
        Assert.Equal(100, EngineSettings.MaxAvailableCharacteristicPoints);
    }

    [Fact]
    public void MinMatureSize_IsLessThanMax()
    {
        Assert.True(EngineSettings.MinMatureSize < EngineSettings.MaxMatureSize);
    }

    [Fact]
    public void GridCellDimensions_ArePowersOfTwo()
    {
        Assert.Equal(1 << EngineSettings.GridWidthPowerOfTwo, EngineSettings.GridCellWidth);
        Assert.Equal(1 << EngineSettings.GridHeightPowerOfTwo, EngineSettings.GridCellHeight);
    }

    [Fact]
    public void DamageToKillPerUnitOfRadius_IsPositive()
    {
        Assert.True(EngineSettings.DamageToKillPerUnitOfRadius > 0);
    }

    [Fact]
    public void EngineSettingsAsserts_DoNotThrow()
    {
        // All Debug.Assert calls should pass
        EngineSettings.EngineSettingsAsserts();
    }

    [Fact]
    public void MaxEnergyBasePerUnitRadius_IsPositive()
    {
        Assert.True(EngineSettings.MaxEnergyBasePerUnitRadius > 0);
    }

    [Fact]
    public void TicksToIncubate_IsPositive()
    {
        Assert.True(EngineSettings.TicksToIncubate > 0);
    }

    [Fact]
    public void CarnivoreAttackDefendMultiplier_IsGreaterThanOne()
    {
        Assert.True(EngineSettings.CarnivoreAttackDefendMultiplier > 1);
    }

    [Fact]
    public void SpeedBase_IsLessThanSpeedMaximum()
    {
        Assert.True(EngineSettings.SpeedBase < EngineSettings.SpeedMaximum);
    }

    [Fact]
    public void BaseEyesightRadius_IsPositive()
    {
        Assert.True(EngineSettings.BaseEyesightRadius > 0);
    }
}
