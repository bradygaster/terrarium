// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>
/// Arguments for the Teleported event.
/// </summary>
public class TeleportedEventArgs : OrganismEventArgs
{
    public TeleportedEventArgs(bool localOnly)
    {
        LocalTeleport = localOnly;
    }

    public bool LocalTeleport { get; private set; }

    public override string ToString()
    {
        return string.Format("#Teleported - (Local = {0})", LocalTeleport);
    }
}
