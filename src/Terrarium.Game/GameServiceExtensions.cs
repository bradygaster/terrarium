// Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Terrarium.Game.Hosting;
using Terrarium.Game.Networking;
using Terrarium.Game.Rendering;
using Terrarium.Game.Services;

namespace Terrarium.Game;

/// <summary>
/// Extension methods for registering Terrarium game engine services.
/// </summary>
public static class GameServiceExtensions
{
    /// <summary>
    /// Registers <see cref="GameEngine"/>, <see cref="PopulationData"/>,
    /// and related game services with the DI container.
    /// </summary>
    public static IServiceCollection AddTerrariumGameEngine(this IServiceCollection services)
    {
        services.AddSingleton<PopulationData>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<PopulationData>>();
            return new PopulationData(reportData: true, logger);
        });

        services.AddSingleton<IGameEngine, GameEngine>();
        services.AddSingleton<AssemblyValidator>();
        services.AddSingleton<CreatureValidator>();
        services.AddSingleton<GameRenderBridge>();
        services.AddSingleton<GameServiceBridge>();

        // Game state persistence
        services.AddSingleton<IGameStatePersistence, GameStatePersistence>();

        return services;
    }

    /// <summary>
    /// Registers <see cref="NetworkEngine"/> and networking services.
    /// </summary>
    public static IServiceCollection AddTerrariumNetworking(this IServiceCollection services,
        Action<NetworkEngineOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddSingleton<NetworkEngineOptions>(sp =>
        {
            var options = new NetworkEngineOptions { HubUrl = "https+http://server/terrarium" };
            configure?.Invoke(options);
            return options;
        });

        services.AddSingleton<INetworkEngine, NetworkEngine>();

        return services;
    }
}
