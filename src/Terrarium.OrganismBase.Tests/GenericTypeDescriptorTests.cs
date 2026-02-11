using System.ComponentModel;
using OrganismBase;
using Xunit;

namespace Terrarium.OrganismBase.Tests;

public class GenericTypeDescriptorTests
{
    [Fact]
    public void Constructor_SetsObject()
    {
        var obj = new { Name = "Test" };
        var descriptor = new GenericTypeDescriptor(obj);
        Assert.NotNull(descriptor);
    }

    [Fact]
    public void GetProperties_ReturnsProperties()
    {
        var obj = new TestObject { Value = 42 };
        ICustomTypeDescriptor descriptor = new GenericTypeDescriptor(obj);
        var props = descriptor.GetProperties();
        Assert.True(props.Count > 0);
    }

    [Fact]
    public void GetPropertyOwner_ReturnsWrappedObject()
    {
        var obj = new TestObject();
        ICustomTypeDescriptor descriptor = new GenericTypeDescriptor(obj);
        var owner = descriptor.GetPropertyOwner(null);
        Assert.Same(obj, owner);
    }

    [Fact]
    public void SetObject_ChangesWrappedObject()
    {
        var obj1 = new TestObject { Value = 1 };
        var obj2 = new TestObject { Value = 2 };
        var descriptor = new GenericTypeDescriptor(obj1);
        descriptor.SetObject(obj2);
        var owner = ((ICustomTypeDescriptor)descriptor).GetPropertyOwner(null);
        Assert.Same(obj2, owner);
    }

    [Fact]
    public void GetAttributes_ReturnsEmptyCollection()
    {
        ICustomTypeDescriptor descriptor = new GenericTypeDescriptor(new TestObject());
        var attrs = descriptor.GetAttributes();
        Assert.NotNull(attrs);
    }

    [Fact]
    public void GetClassName_ReturnsNull()
    {
        ICustomTypeDescriptor descriptor = new GenericTypeDescriptor(new TestObject());
        Assert.Null(descriptor.GetClassName());
    }

    [Fact]
    public void GetEvents_ReturnsEmpty()
    {
        ICustomTypeDescriptor descriptor = new GenericTypeDescriptor(new TestObject());
        Assert.Equal(EventDescriptorCollection.Empty, descriptor.GetEvents());
    }

    private class TestObject
    {
        public int Value { get; set; }
    }
}
