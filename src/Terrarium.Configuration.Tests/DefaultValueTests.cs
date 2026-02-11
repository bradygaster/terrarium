using Terrarium.Configuration;
using Xunit;

namespace Terrarium.Configuration.Tests;

/// <summary>
/// Tests that default values are correct and match legacy GameConfig behavior.
/// </summary>
public class DefaultValueTests
{
    [Fact]
    public void WebRoot_Defaults_To_TerrariumServer()
    {
        // Legacy: "http://www.terrariumserver.com" in userconfig.xml
        var settings = new GameSettings();
        Assert.Equal("http://www.terrariumserver.com", settings.WebRoot);
    }

    [Fact]
    public void StyleName_Defaults_To_Graphite()
    {
        // Legacy: <styleName>Graphite</styleName> in userconfig.xml
        var settings = new GameSettings();
        Assert.Equal("Graphite", settings.StyleName);
    }

    [Fact]
    public void CpuThrottle_Defaults_To_100()
    {
        // Legacy: int i = 100 in CpuThrottle getter
        var settings = new GameSettings();
        Assert.Equal(100, settings.CpuThrottle);
    }

    [Fact]
    public void DrawScreen_Defaults_To_True()
    {
        // Legacy: new CachedBooleanConfig("drawScreen", true)
        var settings = new GameSettings();
        Assert.True(settings.DrawScreen);
    }

    [Fact]
    public void LoggingMode_Defaults_To_Empty()
    {
        var settings = new GameSettings();
        Assert.Equal(string.Empty, settings.LoggingMode);
    }

    [Fact]
    public void PeerList_Defaults_To_Empty()
    {
        var settings = new GameSettings();
        Assert.Equal(string.Empty, settings.PeerList);
    }

    [Fact]
    public void LocalIPAddress_Defaults_To_Empty()
    {
        var settings = new GameSettings();
        Assert.Equal(string.Empty, settings.LocalIPAddress);
    }

    [Fact]
    public void UserCountry_Defaults_To_Unknown()
    {
        var settings = new GameSettings();
        Assert.Equal("<Unknown>", settings.UserCountry);
    }

    [Fact]
    public void UserState_Defaults_To_Unknown()
    {
        var settings = new GameSettings();
        Assert.Equal("<Unknown>", settings.UserState);
    }

    [Fact]
    public void UserEmail_Defaults_To_Empty()
    {
        var settings = new GameSettings();
        Assert.Equal(string.Empty, settings.UserEmail);
    }

    [Fact]
    public void BlockedVersion_Defaults_To_Empty()
    {
        var settings = new GameSettings();
        Assert.Equal(string.Empty, settings.BlockedVersion);
    }

    [Fact]
    public void ShowErrors_Is_True_When_DemoMode_Is_False()
    {
        var settings = new GameSettings { DemoMode = false };
        Assert.True(settings.ShowErrors);
    }

    [Fact]
    public void ShowErrors_Is_False_When_DemoMode_Is_True()
    {
        var settings = new GameSettings { DemoMode = true };
        Assert.False(settings.ShowErrors);
    }

    [Fact]
    public void AllowUpdates_Is_True_When_DemoMode_Is_False()
    {
        var settings = new GameSettings { DemoMode = false };
        Assert.True(settings.AllowUpdates);
    }

    [Fact]
    public void AllowUpdates_Is_False_When_DemoMode_Is_True()
    {
        var settings = new GameSettings { DemoMode = true };
        Assert.False(settings.AllowUpdates);
    }
}
