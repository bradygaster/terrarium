// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;

namespace OrganismBase;

/// <summary>
/// Collection of AttackedEventArgs for a creature's tick.
/// </summary>
public class AttackedEventArgsCollection
{
    private readonly List<AttackedEventArgs> _innerList = new List<AttackedEventArgs>();

    internal AttackedEventArgsCollection() { }

    internal Boolean IsImmutable { get; private set; }

    public AttackedEventArgs this[int index] => _innerList[index];

    public int Count => _innerList.Count;

    internal void MakeImmutable()
    {
        IsImmutable = true;
    }

    public void Add(AttackedEventArgs attackedEventArgs)
    {
        if (IsImmutable)
        {
            throw new ApplicationException("Object is immutable.");
        }

        _innerList.Add(attackedEventArgs);
    }

    public IEnumerator<AttackedEventArgs> GetEnumerator() => _innerList.GetEnumerator();
}
