// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using OrganismBase;

namespace Terrarium.Game.Hosting;

/// <summary>
/// Interface for organism scheduling. Manages turn-taking for all
/// organisms in the terrarium, enforcing fair CPU time distribution.
/// </summary>
public interface IOrganismScheduler
{
    /// <summary>
    /// All organisms currently managed by the scheduler.
    /// </summary>
    IReadOnlyCollection<Organism> Organisms { get; }

    /// <summary>
    /// The current world state used by the scheduler.
    /// Updated at the end of each tick by the game engine.
    /// </summary>
    WorldState? CurrentState { get; set; }

    /// <summary>
    /// Creates and registers a new organism of the given type.
    /// </summary>
    /// <param name="creatureType">The CLR type of the creature to instantiate.</param>
    /// <param name="id">The organism ID to assign.</param>
    /// <returns>The created organism instance, or null if creation failed.</returns>
    Organism? Create(Type creatureType, string id);

    /// <summary>
    /// Removes an organism from the scheduler.
    /// </summary>
    /// <param name="id">The organism ID to remove.</param>
    void Remove(string id);

    /// <summary>
    /// Processes one scheduling tick: gives a slice of organisms their turn.
    /// Called 5 times per game tick (phases 0-4), each time processing
    /// roughly 1/5 of all organisms.
    /// </summary>
    void Tick();

    /// <summary>
    /// Gets the organism instance by ID.
    /// </summary>
    /// <param name="id">The organism ID.</param>
    /// <returns>The organism, or null if not found.</returns>
    Organism? GetOrganism(string id);

    /// <summary>
    /// Gets the quanta tracking for a specific organism.
    /// </summary>
    /// <param name="id">The organism ID.</param>
    /// <returns>The quanta tracker, or null if not found.</returns>
    OrganismQuanta? GetQuanta(string id);

    /// <summary>
    /// Destroys the scheduler and releases all resources.
    /// </summary>
    void Destroy();
}
