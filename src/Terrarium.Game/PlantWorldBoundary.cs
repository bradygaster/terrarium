// Copyright (c) Microsoft Corporation.  All rights reserved.

using OrganismBase;

namespace Terrarium.Game;

/// <summary>
/// Plant-specific world boundary implementing IPlantWorldBoundary.
/// </summary>
public class PlantWorldBoundary : OrganismWorldBoundary, IPlantWorldBoundary
{
    private readonly Func<WorldState> _currentStateAccessor;

    internal PlantWorldBoundary(
        Organism plant, string id,
        Func<WorldState> currentStateAccessor,
        Func<int> worldWidthAccessor,
        Func<int> worldHeightAccessor)
        : base(plant, id, currentStateAccessor, worldWidthAccessor, worldHeightAccessor)
    {
        _currentStateAccessor = currentStateAccessor;
    }

    public PlantState CurrentPlantState
        => (PlantState)_currentStateAccessor().GetOrganismState(ID)!;
}
