// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>Determines the skin used to display an animal on screen.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class AnimalSkinAttribute : Attribute
{
    public AnimalSkinAttribute(string skin)
    {
        SkinFamily = AnimalSkinFamily.Ant;
        Skin = skin;
    }

    public AnimalSkinAttribute(AnimalSkinFamily skinFamily)
    {
        Skin = string.Empty;
        SkinFamily = skinFamily;
    }

    public AnimalSkinAttribute(AnimalSkinFamily skinFamily, string skin)
    {
        SkinFamily = skinFamily;
        Skin = skin;
    }

    public string Skin { get; private set; }
    public AnimalSkinFamily SkinFamily { get; private set; }
}
