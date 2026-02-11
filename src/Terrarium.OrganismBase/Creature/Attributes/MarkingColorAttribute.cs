// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Drawing;

namespace OrganismBase;

/// <summary>Determines the color used for special markings on the organism.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class MarkingColorAttribute : Attribute
{
    public MarkingColorAttribute(KnownColor markingColor)
    {
        MarkingColor = markingColor;
    }

    public KnownColor MarkingColor { get; private set; }
}
