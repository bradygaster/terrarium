// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>Determines the skin used to display a plant on screen.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class PlantSkinAttribute : Attribute
{
    public PlantSkinAttribute(string skin)
    {
        SkinFamily = PlantSkinFamily.Plant;
        Skin = skin;
    }

    public PlantSkinAttribute(PlantSkinFamily skinFamily)
    {
        Skin = string.Empty;
        SkinFamily = skinFamily;
    }

    public PlantSkinAttribute(PlantSkinFamily skinFamily, string skin)
    {
        SkinFamily = skinFamily;
        Skin = skin;
    }

    public string Skin { get; private set; }
    public PlantSkinFamily SkinFamily { get; private set; }
}
