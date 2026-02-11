// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Terrarium.Game;

/// <summary>
/// Contains world state + the actions organisms want to perform next.
/// </summary>
public class WorldVector
{
    private TickActions? _currentActions;

    public WorldVector(WorldState state)
    {
        if (!state.IsImmutable)
            throw new InvalidOperationException("WorldState must be immutable to be added to vector.");
        State = state;
    }

    public TickActions Actions
    {
        get => _currentActions!;
        set => _currentActions = value ?? throw new InvalidOperationException("Actions can't be null.");
    }

    public WorldState State { get; private set; }
}
