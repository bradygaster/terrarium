// Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Terrarium.Configuration;

/// <summary>
/// Extension methods to register Terrarium configuration services.
/// </summary>
public static class ConfigurationServiceExtensions
{
    /// <summary>
    /// Adds <see cref="GameSettings"/> via <c>IOptions&lt;GameSettings&gt;</c> with
    /// DataAnnotations validation, and registers <see cref="ErrorLog"/> as a singleton.
    /// </summary>
    public static IServiceCollection AddTerrariumConfiguration(this IServiceCollection services)
    {
        services.AddOptions<GameSettings>()
            .BindConfiguration(GameSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ErrorLog>();

        return services;
    }
}
