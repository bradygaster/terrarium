using OrganismBase;
using Xunit;

namespace Terrarium.OrganismBase.Tests;

public class OrganismEventResultsTests
{
    [Fact]
    public void NewInstance_AllPropertiesNull()
    {
        var events = new OrganismEventResults();
        Assert.Null(events.Born);
        Assert.Null(events.ReproduceCompleted);
        Assert.Null(events.Teleported);
        Assert.Null(events.MoveCompleted);
        Assert.Null(events.AttackCompleted);
        Assert.Null(events.EatCompleted);
        Assert.Null(events.DefendCompleted);
        Assert.NotNull(events.AttackedEvents);
        Assert.Equal(0, events.AttackedEvents.Count);
    }

    [Fact]
    public void MakeImmutable_PreventsModification()
    {
        var events = new OrganismEventResults();
        events.MakeImmutable();
        Assert.True(events.IsImmutable);
    }

    [Fact]
    public void Born_OnImmutable_Throws()
    {
        var events = new OrganismEventResults();
        events.MakeImmutable();
        Assert.Throws<ApplicationException>(() => events.Born = new BornEventArgs(null));
    }

    [Fact]
    public void ReproduceCompleted_OnImmutable_Throws()
    {
        var events = new OrganismEventResults();
        events.MakeImmutable();
        Assert.Throws<ApplicationException>(() => events.ReproduceCompleted = null);
    }

    [Fact]
    public void Teleported_OnImmutable_Throws()
    {
        var events = new OrganismEventResults();
        events.MakeImmutable();
        Assert.Throws<ApplicationException>(() => events.Teleported = null);
    }

    [Fact]
    public void MoveCompleted_OnImmutable_Throws()
    {
        var events = new OrganismEventResults();
        events.MakeImmutable();
        Assert.Throws<ApplicationException>(() => events.MoveCompleted = null);
    }

    [Fact]
    public void AttackCompleted_OnImmutable_Throws()
    {
        var events = new OrganismEventResults();
        events.MakeImmutable();
        Assert.Throws<ApplicationException>(() => events.AttackCompleted = null);
    }

    [Fact]
    public void EatCompleted_OnImmutable_Throws()
    {
        var events = new OrganismEventResults();
        events.MakeImmutable();
        Assert.Throws<ApplicationException>(() => events.EatCompleted = null);
    }

    [Fact]
    public void DefendCompleted_OnImmutable_Throws()
    {
        var events = new OrganismEventResults();
        events.MakeImmutable();
        Assert.Throws<ApplicationException>(() => events.DefendCompleted = null);
    }

    [Fact]
    public void Born_CanSetOnMutable()
    {
        var events = new OrganismEventResults();
        var born = new BornEventArgs(new byte[] { 1, 2, 3 });
        events.Born = born;
        Assert.Same(born, events.Born);
    }
}
