// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections;
using OrganismBase;

namespace Terrarium.Game;

/// <summary>
/// Animal-specific world boundary implementing IAnimalWorldBoundary.
/// </summary>
public class AnimalWorldBoundary : OrganismWorldBoundary, IAnimalWorldBoundary
{
    private readonly Func<WorldState> _currentStateAccessor;

    internal AnimalWorldBoundary(
        Organism animal, string id,
        Func<WorldState> currentStateAccessor,
        Func<int> worldWidthAccessor,
        Func<int> worldHeightAccessor)
        : base(animal, id, currentStateAccessor, worldWidthAccessor, worldHeightAccessor)
    {
        _currentStateAccessor = currentStateAccessor;
    }

    public AnimalState CurrentAnimalState
        => (AnimalState)_currentStateAccessor().GetOrganismState(ID)!;

    public ArrayList Scan()
    {
        var worldState = _currentStateAccessor();
        var thisState = worldState.GetOrganismState(ID)!;
        var found = worldState.FindOrganismsInView(CurrentAnimalState,
            ((AnimalSpecies)thisState.Species).EyesightRadius);
        var foundList = new ArrayList(found);
        foundList.Remove(thisState);
        for (var i = 0; i < foundList.Count;)
        {
            var state = (OrganismState)foundList[i]!;
            if (state is AnimalState && state.IsAlive)
            {
                var invisible = Organism.OrganismRandom.Next(1, 100);
                if (invisible <= ((AnimalSpecies)state.Species).InvisibleOdds)
                {
                    foundList.Remove(state);
                    Organism.WriteTrace("#Camouflage hid animal from organism");
                    continue;
                }
            }
            i++;
        }
        return foundList;
    }

    public OrganismState? LookFor(OrganismState organismState)
    {
        if (organismState == null) throw new ArgumentNullException(nameof(organismState));
        var target = LookForNoCamouflage(organismState);
        if (target is AnimalState)
        {
            var inv = Organism.OrganismRandom.Next(1, 100);
            if (inv <= ((AnimalSpecies)target.Species).InvisibleOdds)
            {
                Organism.WriteTrace("#Camouflage hid animal from organism");
                return null;
            }
        }
        return target;
    }

    public OrganismState? LookForNoCamouflage(OrganismState organismState)
    {
        if (organismState == null) return null;
        var ws = _currentStateAccessor();
        var os = ws.GetOrganismState(organismState.ID);
        var thisOrg = CurrentAnimalState;
        if (os == null || !thisOrg.IsWithinRect(((AnimalSpecies)thisOrg.Species).EyesightRadius, os))
            return null;
        return os;
    }

    public OrganismState? RefreshState(string organismID)
    {
        if (organismID == null) throw new ArgumentNullException(nameof(organismID));
        var org = _currentStateAccessor().GetOrganismState(organismID);
        if (org != null) org = LookFor(org);
        return org;
    }
}
