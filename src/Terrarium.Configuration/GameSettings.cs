// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Terrarium.Configuration;

/// <summary>
/// Strongly typed game configuration. Replaces the legacy static <c>GameConfig</c> class.
/// Bind to the <c>"Game"</c> section of appsettings.json and validate via DataAnnotations.
/// </summary>
public sealed class GameSettings
{
    public const string SectionName = "Game";

    // --- Rendering ---

    public bool BackgroundGrid { get; set; }

    public bool BoundingBoxes { get; set; }

    public bool DestinationLines { get; set; }

    public bool DrawScreen { get; set; } = true;

    public bool LargeGraphicsMode { get; set; }

    // --- Behavior ---

    public bool DemoMode { get; set; }

    public bool SkipVersionCheck { get; set; }

    public bool StartFullscreen { get; set; }

    public bool ScreenSaverSpanMonitors { get; set; }

    public bool UseSimpleScreenSaver { get; set; }

    // --- Networking ---

    public bool EnableNat { get; set; }

    public bool UseConfigForDiscovery { get; set; }

    public string PeerList { get; set; } = string.Empty;

    public string LocalIPAddress { get; set; } = string.Empty;

    public string WebRoot { get; set; } = "http://www.terrariumserver.com";

    // --- User info ---

    public string UserCountry { get; set; } = "<Unknown>";

    public string UserState { get; set; } = "<Unknown>";

    public string UserEmail { get; set; } = string.Empty;

    // --- Display / Theme ---

    public string StyleName { get; set; } = "Graphite";

    public string LoggingMode { get; set; } = string.Empty;

    // --- Performance ---

    [Range(50, 200)]
    public int CpuThrottle { get; set; } = 100;

    // --- Version ---

    public string BlockedVersion { get; set; } = string.Empty;

    // --- Derived (read-only) ---

    public bool ShowErrors => !DemoMode;

    public bool AllowUpdates => !DemoMode;
}
