// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Terrarium.Game;

/// <summary>
/// Defines the networking mode for the Terrarium ecosystem.
/// </summary>
public enum EcosystemMode
{
    /// <summary>
    /// Local-only mode: engine runs without SignalR, no server connection needed.
    /// All network operations (heartbeat, population reporting, teleportation) are skipped.
    /// </summary>
    LocalOnly = 0,

    /// <summary>
    /// Networked mode: full SignalR connectivity with server and peer communication.
    /// All network operations are active (heartbeat, population reporting, teleportation).
    /// </summary>
    Networked = 1
}
