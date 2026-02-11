using Microsoft.Extensions.Configuration;
using Terrarium.Configuration;
using Xunit;

namespace Terrarium.Configuration.Tests;

/// <summary>
/// Tests that configuration loads correctly from JSON sources.
/// Validates that appsettings.json structure binds to strongly-typed GameSettings.
/// </summary>
public class ConfigLoadingTests
{
    private static IConfiguration BuildConfigFromJson(string json)
    {
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        return new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();
    }

    [Fact]
    public void Can_Load_GameSettings_From_Json()
    {
        var json = """
        {
            "Game": {
                "WebRoot": "https://terrarium.example.com",
                "StyleName": "Glass",
                "StartFullscreen": true,
                "CpuThrottle": 150
            }
        }
        """;

        var config = BuildConfigFromJson(json);
        var settings = new GameSettings();
        config.GetSection(GameSettings.SectionName).Bind(settings);

        Assert.Equal("https://terrarium.example.com", settings.WebRoot);
        Assert.Equal("Glass", settings.StyleName);
        Assert.True(settings.StartFullscreen);
        Assert.Equal(150, settings.CpuThrottle);
    }

    [Fact]
    public void Missing_Section_Uses_Default_Values()
    {
        var json = "{}";

        var config = BuildConfigFromJson(json);
        var settings = new GameSettings();
        config.GetSection(GameSettings.SectionName).Bind(settings);

        Assert.Equal("http://www.terrariumserver.com", settings.WebRoot);
        Assert.Equal("Graphite", settings.StyleName);
        Assert.False(settings.StartFullscreen);
        Assert.Equal(100, settings.CpuThrottle);
    }

    [Fact]
    public void Partial_Json_Preserves_Unset_Defaults()
    {
        var json = """
        {
            "Game": {
                "StyleName": "Metallic"
            }
        }
        """;

        var config = BuildConfigFromJson(json);
        var settings = new GameSettings();
        config.GetSection(GameSettings.SectionName).Bind(settings);

        Assert.Equal("Metallic", settings.StyleName);
        Assert.Equal("http://www.terrariumserver.com", settings.WebRoot);
        Assert.False(settings.StartFullscreen);
        Assert.True(settings.DrawScreen);
        Assert.Equal(100, settings.CpuThrottle);
    }

    [Fact]
    public void Boolean_Options_Bind_From_Json_Strings()
    {
        var json = """
        {
            "Game": {
                "DemoMode": "true",
                "EnableNat": "True",
                "DrawScreen": "false"
            }
        }
        """;

        var config = BuildConfigFromJson(json);
        var settings = new GameSettings();
        config.GetSection(GameSettings.SectionName).Bind(settings);

        Assert.True(settings.DemoMode);
        Assert.True(settings.EnableNat);
        Assert.False(settings.DrawScreen);
    }

    [Fact]
    public void All_Boolean_Defaults_Match_Legacy_GameConfig()
    {
        // Legacy GameConfig defaults from Client/Configuration/Classes/Config/GameConfig.cs
        var settings = new GameSettings();

        Assert.False(settings.StartFullscreen);
        Assert.False(settings.ScreenSaverSpanMonitors);
        Assert.False(settings.LargeGraphicsMode);
        Assert.False(settings.SkipVersionCheck);
        Assert.False(settings.DemoMode);
        Assert.True(settings.DrawScreen);       // DrawScreen defaults to true
        Assert.False(settings.BackgroundGrid);
        Assert.False(settings.BoundingBoxes);
        Assert.False(settings.DestinationLines);
        Assert.False(settings.EnableNat);
        Assert.False(settings.UseConfigForDiscovery);
    }

    [Fact]
    public void SectionName_Is_Game()
    {
        Assert.Equal("Game", GameSettings.SectionName);
    }

    [Fact]
    public void Networking_Properties_Bind_From_Json()
    {
        var json = """
        {
            "Game": {
                "EnableNat": true,
                "UseConfigForDiscovery": true,
                "PeerList": "192.168.1.1,192.168.1.2",
                "LocalIPAddress": "10.0.0.5"
            }
        }
        """;

        var config = BuildConfigFromJson(json);
        var settings = new GameSettings();
        config.GetSection(GameSettings.SectionName).Bind(settings);

        Assert.True(settings.EnableNat);
        Assert.True(settings.UseConfigForDiscovery);
        Assert.Equal("192.168.1.1,192.168.1.2", settings.PeerList);
        Assert.Equal("10.0.0.5", settings.LocalIPAddress);
    }
}
