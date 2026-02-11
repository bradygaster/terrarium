using OrganismBase;
using Xunit;

namespace Terrarium.OrganismBase.Tests;

public class PendingActionsTests
{
    [Fact]
    public void NewPendingActions_AllActionsNull()
    {
        var actions = new PendingActions();
        Assert.Null(actions.DefendAction);
        Assert.Null(actions.MoveToAction);
        Assert.Null(actions.AttackAction);
        Assert.Null(actions.EatAction);
        Assert.Null(actions.ReproduceAction);
        Assert.False(actions.IsImmutable);
    }

    [Fact]
    public void MakeImmutable_PreventsModification()
    {
        var actions = new PendingActions();
        actions.MakeImmutable();
        Assert.True(actions.IsImmutable);
    }

    [Fact]
    public void SetDefendAction_OnImmutable_Throws()
    {
        var actions = new PendingActions();
        actions.MakeImmutable();
        Assert.Throws<ApplicationException>(() => actions.SetDefendAction(null));
    }

    [Fact]
    public void SetMoveToAction_OnImmutable_Throws()
    {
        var actions = new PendingActions();
        actions.MakeImmutable();
        Assert.Throws<ApplicationException>(() => actions.SetMoveToAction(null));
    }

    [Fact]
    public void SetAttackAction_OnImmutable_Throws()
    {
        var actions = new PendingActions();
        actions.MakeImmutable();
        Assert.Throws<ApplicationException>(() => actions.SetAttackAction(null));
    }

    [Fact]
    public void SetEatAction_OnImmutable_Throws()
    {
        var actions = new PendingActions();
        actions.MakeImmutable();
        Assert.Throws<ApplicationException>(() => actions.SetEatAction(null));
    }

    [Fact]
    public void SetReproduceAction_OnImmutable_Throws()
    {
        var actions = new PendingActions();
        actions.MakeImmutable();
        Assert.Throws<ApplicationException>(() => actions.SetReproduceAction(null));
    }

    [Fact]
    public void SetActions_Null_ClearsAction()
    {
        var actions = new PendingActions();
        actions.SetDefendAction(null);
        Assert.Null(actions.DefendAction);
        actions.SetMoveToAction(null);
        Assert.Null(actions.MoveToAction);
        actions.SetAttackAction(null);
        Assert.Null(actions.AttackAction);
        actions.SetEatAction(null);
        Assert.Null(actions.EatAction);
        actions.SetReproduceAction(null);
        Assert.Null(actions.ReproduceAction);
    }
}
