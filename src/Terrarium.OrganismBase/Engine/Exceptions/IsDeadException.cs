// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace OrganismBase;

/// <summary>You can only defend against alive animals.</summary>
public class IsDeadException : OrganismException
{
    internal IsDeadException() : base("You can only defend against alive animals.") { }
}
