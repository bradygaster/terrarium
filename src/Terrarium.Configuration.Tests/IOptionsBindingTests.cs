using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Terrarium.Configuration;
using Xunit;

namespace Terrarium.Configuration.Tests;

/// <summary>
/// Tests that IOptions binding works correctly via the DI container.
/// Verifies the AddTerrariumConfiguration() extension method pattern.
/// </summary>
public class IOptionsBindingTests
{
    private static ServiceProvider BuildServiceProvider(string json)
    {
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.Configure<GameSettings>(config.GetSection(GameSettings.SectionName));

        return services.BuildServiceProvider();
    }

    [Fact]
    public void IOptions_GameSettings_Resolves_From_DI()
    {
        var json = """
        {
            "Game": {
                "WebRoot": "https://terrarium.example.com",
                "CpuThrottle": 125
            }
        }
        """;

        using var provider = BuildServiceProvider(json);
        var settings = provider.GetRequiredService<IOptions<GameSettings>>().Value;

        Assert.Equal("https://terrarium.example.com", settings.WebRoot);
        Assert.Equal(125, settings.CpuThrottle);
    }

    [Fact]
    public void IOptions_Uses_Defaults_When_Section_Missing()
    {
        var json = "{}";

        using var provider = BuildServiceProvider(json);
        var settings = provider.GetRequiredService<IOptions<GameSettings>>().Value;

        Assert.Equal("http://www.terrariumserver.com", settings.WebRoot);
        Assert.Equal("Graphite", settings.StyleName);
        Assert.Equal(100, settings.CpuThrottle);
        Assert.True(settings.DrawScreen);
    }

    [Fact]
    public void IOptionsSnapshot_Allows_Per_Request_Resolution()
    {
        var json = """
        {
            "Game": {
                "StyleName": "Crystal"
            }
        }
        """;

        using var provider = BuildServiceProvider(json);

        using var scope = provider.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<GameSettings>>().Value;

        Assert.Equal("Crystal", settings.StyleName);
    }

    [Fact]
    public void All_Boolean_Properties_Bind_Correctly_Via_IOptions()
    {
        var json = """
        {
            "Game": {
                "StartFullscreen": true,
                "ScreenSaverSpanMonitors": true,
                "LargeGraphicsMode": true,
                "SkipVersionCheck": true,
                "DemoMode": true,
                "DrawScreen": false,
                "BackgroundGrid": true,
                "BoundingBoxes": true,
                "DestinationLines": true,
                "EnableNat": true,
                "UseConfigForDiscovery": true,
                "UseSimpleScreenSaver": true
            }
        }
        """;

        using var provider = BuildServiceProvider(json);
        var settings = provider.GetRequiredService<IOptions<GameSettings>>().Value;

        Assert.True(settings.StartFullscreen);
        Assert.True(settings.ScreenSaverSpanMonitors);
        Assert.True(settings.LargeGraphicsMode);
        Assert.True(settings.SkipVersionCheck);
        Assert.True(settings.DemoMode);
        Assert.False(settings.DrawScreen);
        Assert.True(settings.BackgroundGrid);
        Assert.True(settings.BoundingBoxes);
        Assert.True(settings.DestinationLines);
        Assert.True(settings.EnableNat);
        Assert.True(settings.UseConfigForDiscovery);
        Assert.True(settings.UseSimpleScreenSaver);
    }

    [Fact]
    public void AddTerrariumConfiguration_Registers_GameSettings()
    {
        var json = """
        {
            "Game": {
                "WebRoot": "https://game.terrarium.dev",
                "DemoMode": true
            }
        }
        """;

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddTerrariumConfiguration();

        using var provider = services.BuildServiceProvider();

        var settings = provider.GetRequiredService<IOptions<GameSettings>>().Value;
        Assert.Equal("https://game.terrarium.dev", settings.WebRoot);
        Assert.True(settings.DemoMode);
        Assert.False(settings.ShowErrors);
        Assert.False(settings.AllowUpdates);
    }

    [Fact]
    public void AddTerrariumConfiguration_Registers_ErrorLog()
    {
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{}"));
        var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddLogging();
        services.AddTerrariumConfiguration();

        using var provider = services.BuildServiceProvider();

        var errorLog = provider.GetService<ErrorLog>();
        Assert.NotNull(errorLog);
    }

    [Fact]
    public void User_Info_Properties_Bind_From_Json()
    {
        var json = """
        {
            "Game": {
                "UserCountry": "United States",
                "UserState": "Washington",
                "UserEmail": "dev@terrarium.dev"
            }
        }
        """;

        using var provider = BuildServiceProvider(json);
        var settings = provider.GetRequiredService<IOptions<GameSettings>>().Value;

        Assert.Equal("United States", settings.UserCountry);
        Assert.Equal("Washington", settings.UserState);
        Assert.Equal("dev@terrarium.dev", settings.UserEmail);
    }
}
