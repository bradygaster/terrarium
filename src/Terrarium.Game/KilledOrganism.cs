// Copyright (c) Microsoft Corporation.  All rights reserved.

using OrganismBase;

namespace Terrarium.Game;

/// <summary>
/// Represents an organism that has been killed and is pending removal.
/// </summary>
public class KilledOrganism
{
    public KilledOrganism(string id, PopulationChangeReason reason, string extraInformation)
    { ID = id; DeathReason = reason; ExtraInformation = extraInformation; }

    public KilledOrganism(string id, PopulationChangeReason reason)
    { ID = id; DeathReason = reason; ExtraInformation = ""; }

    public KilledOrganism(OrganismState state)
    { ID = state.ID; DeathReason = state.DeathReason; ExtraInformation = ""; }

    public string ID { get; private set; }
    public string ExtraInformation { get; private set; }
    public PopulationChangeReason DeathReason { get; private set; }
}
