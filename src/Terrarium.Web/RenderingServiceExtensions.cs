// Copyright (c) Microsoft Corporation.  All rights reserved.

using Terrarium.Web.Rendering;

namespace Terrarium.Web;

/// <summary>
/// Extension methods for registering Terrarium renderer services.
/// </summary>
public static class RenderingServiceExtensions
{
    /// <summary>
    /// Registers <see cref="IGameRenderer"/> with the <see cref="CanvasGameRenderer"/>
    /// implementation as a scoped service (one per Blazor circuit).
    /// </summary>
    public static IServiceCollection AddTerrariumRenderer(this IServiceCollection services)
    {
        services.AddScoped<IGameRenderer, CanvasGameRenderer>();

        return services;
    }
}
