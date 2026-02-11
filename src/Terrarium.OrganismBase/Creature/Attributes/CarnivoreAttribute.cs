// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>Chooses whether your animal is an herbivore or a carnivore.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class CarnivoreAttribute : Attribute
{
    public CarnivoreAttribute(Boolean isCarnivore)
    {
        IsCarnivore = isCarnivore;
    }

    public bool IsCarnivore { get; private set; }
}
