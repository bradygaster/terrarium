// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>This organism is not visible.</summary>
public class NotVisibleException : OrganismException
{
    internal NotVisibleException() : base("This organism is not visible.") { }
}
