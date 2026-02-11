// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace OrganismBase;

/// <summary>
/// Required attribute to identify a creature's author.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, Inherited = true, AllowMultiple = false)]
public sealed class AuthorInformationAttribute : Attribute
{
    public AuthorInformationAttribute(string authorName)
    {
        AuthorName = authorName;
        AuthorEmail = "";
    }

    public AuthorInformationAttribute(string authorName, string authorEmail)
    {
        AuthorName = authorName;
        AuthorEmail = authorEmail;
    }

    public string AuthorName { get; private set; }
    public string AuthorEmail { get; private set; }
}
