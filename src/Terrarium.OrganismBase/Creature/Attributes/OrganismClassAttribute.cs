// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>
/// Identifies the class in the assembly that derives from Plant or Animal.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, Inherited = true, AllowMultiple = false)]
public sealed class OrganismClassAttribute : Attribute
{
    public OrganismClassAttribute(string className)
    {
        ClassName = className;
    }

    public string ClassName { get; private set; }
}
