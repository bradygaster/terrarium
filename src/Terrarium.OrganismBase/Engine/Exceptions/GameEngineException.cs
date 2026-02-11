// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>
/// The base class for all exceptions an organism will receive from the game.
/// </summary>
public class GameEngineException : Exception
{
    public GameEngineException(string message) : base(message) { }
}
