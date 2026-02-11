using OrganismBase;
using Xunit;

namespace Terrarium.OrganismBase.Tests;

public class AttackedEventArgsCollectionTests
{
    [Fact]
    public void NewCollection_IsEmpty()
    {
        // AttackedEventArgsCollection has internal constructor,
        // so we access it through OrganismEventResults
        var events = new OrganismEventResults();
        Assert.Equal(0, events.AttackedEvents.Count);
    }

    [Fact]
    public void Add_IncreasesCount()
    {
        var events = new OrganismEventResults();
        var attacker = new AnimalState("attacker", new MockAnimalSpecies(), 0, EnergyState.Full, 10);
        var args = new AttackedEventArgs(attacker);
        events.AttackedEvents.Add(args);
        Assert.Equal(1, events.AttackedEvents.Count);
    }

    [Fact]
    public void Indexer_ReturnsCorrectItem()
    {
        var events = new OrganismEventResults();
        var attacker = new AnimalState("attacker", new MockAnimalSpecies(), 0, EnergyState.Full, 10);
        var args = new AttackedEventArgs(attacker);
        events.AttackedEvents.Add(args);
        Assert.Same(args, events.AttackedEvents[0]);
    }

    [Fact]
    public void Add_OnImmutable_Throws()
    {
        var events = new OrganismEventResults();
        events.MakeImmutable();
        var attacker = new AnimalState("attacker", new MockAnimalSpecies(), 0, EnergyState.Full, 10);
        var args = new AttackedEventArgs(attacker);
        Assert.Throws<ApplicationException>(() => events.AttackedEvents.Add(args));
    }

    [Fact]
    public void GetEnumerator_IteratesItems()
    {
        var events = new OrganismEventResults();
        var attacker1 = new AnimalState("a1", new MockAnimalSpecies(), 0, EnergyState.Full, 10);
        var attacker2 = new AnimalState("a2", new MockAnimalSpecies(), 0, EnergyState.Full, 10);
        events.AttackedEvents.Add(new AttackedEventArgs(attacker1));
        events.AttackedEvents.Add(new AttackedEventArgs(attacker2));

        int count = 0;
        var enumerator = events.AttackedEvents.GetEnumerator();
        while (enumerator.MoveNext())
            count++;
        Assert.Equal(2, count);
    }
}
