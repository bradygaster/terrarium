// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.ComponentModel;

namespace OrganismBase;

/// <summary>
/// A Custom Type Descriptor class used to expand objects into the Property Browser dialog.
/// </summary>
[TypeConverter((typeof(ExpandableObjectConverter)))]
public class GenericTypeDescriptor : ICustomTypeDescriptor
{
    private object currentObject;
    private PropertyDescriptorCollection? propsCollection;

    public GenericTypeDescriptor(object current)
    {
        currentObject = current;
    }

    AttributeCollection ICustomTypeDescriptor.GetAttributes() => new AttributeCollection(null);
    string? ICustomTypeDescriptor.GetClassName() => null;
    string? ICustomTypeDescriptor.GetComponentName() => null;
    TypeConverter? ICustomTypeDescriptor.GetConverter() => null;
    EventDescriptor? ICustomTypeDescriptor.GetDefaultEvent() => null;
    PropertyDescriptor? ICustomTypeDescriptor.GetDefaultProperty() => null;
    object? ICustomTypeDescriptor.GetEditor(Type editorBaseType) => null;
    EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => EventDescriptorCollection.Empty;
    EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[]? attributes) => EventDescriptorCollection.Empty;

    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() =>
        ((ICustomTypeDescriptor)this).GetProperties(null);

    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[]? attributes)
    {
        if (propsCollection == null)
        {
            propsCollection = TypeDescriptor.GetProperties(currentObject.GetType(), attributes!);
        }
        return propsCollection;
    }

    object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor? pd) => currentObject;

    public void SetObject(object current)
    {
        currentObject = current;
    }
}
