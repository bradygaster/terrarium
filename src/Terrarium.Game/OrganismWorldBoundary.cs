// Copyright (c) Microsoft Corporation.  All rights reserved.

using OrganismBase;

namespace Terrarium.Game;

/// <summary>
/// Implements an organism's view of the world. In the modernized architecture,
/// the boundary holds direct references to state accessors rather than global statics.
/// </summary>
public class OrganismWorldBoundary : IOrganismWorldBoundary
{
    private readonly Func<WorldState> _currentStateAccessor;
    private readonly Func<int> _worldWidthAccessor;
    private readonly Func<int> _worldHeightAccessor;

    internal OrganismWorldBoundary(
        Organism organism, string id,
        Func<WorldState> currentStateAccessor,
        Func<int> worldWidthAccessor,
        Func<int> worldHeightAccessor)
    {
        Organism = organism;
        ID = id;
        _currentStateAccessor = currentStateAccessor;
        _worldWidthAccessor = worldWidthAccessor;
        _worldHeightAccessor = worldHeightAccessor;
    }

    protected Organism Organism { get; set; }

    public OrganismState CurrentOrganismState
        => _currentStateAccessor().GetOrganismState(ID)!;

    public string ID { get; private set; }
    public int WorldWidth => _worldWidthAccessor();
    public int WorldHeight => _worldHeightAccessor();

    protected void SetOrganismID(string id) => ID = id;

    internal static void SetWorldBoundary(
        Organism organism, string id,
        Func<WorldState> currentStateAccessor,
        Func<int> worldWidthAccessor,
        Func<int> worldHeightAccessor)
    {
        if (organism is Animal)
            organism.SetWorldBoundary(new AnimalWorldBoundary(organism, id, currentStateAccessor, worldWidthAccessor, worldHeightAccessor));
        else
            organism.SetWorldBoundary(new PlantWorldBoundary(organism, id, currentStateAccessor, worldWidthAccessor, worldHeightAccessor));
    }
}
