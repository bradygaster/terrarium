// Copyright (c) Microsoft Corporation.  All rights reserved.

using OrganismBase;

namespace Terrarium.Game;

/// <summary>
/// Aggregates all creature actions for a single game tick.
/// </summary>
public class TickActions
{
    private readonly Dictionary<string, MoveToAction> _moveToActions = new();
    private readonly Dictionary<string, AttackAction> _attackActions = new();
    private readonly Dictionary<string, EatAction> _eatActions = new();
    private readonly Dictionary<string, ReproduceAction> _reproduceActions = new();
    private readonly Dictionary<string, DefendAction> _defendActions = new();

    public IReadOnlyDictionary<string, MoveToAction> MoveToActions => new Dictionary<string, MoveToAction>(_moveToActions);
    public IReadOnlyDictionary<string, AttackAction> AttackActions => new Dictionary<string, AttackAction>(_attackActions);
    public IReadOnlyDictionary<string, EatAction> EatActions => new Dictionary<string, EatAction>(_eatActions);
    public IReadOnlyDictionary<string, ReproduceAction> ReproduceActions => new Dictionary<string, ReproduceAction>(_reproduceActions);
    public IReadOnlyDictionary<string, DefendAction> DefendActions => new Dictionary<string, DefendAction>(_defendActions);

    internal void GatherActionsFromOrganisms(IEnumerable<Organism> organisms)
    {
        foreach (var organism in organisms)
        {
            var id = organism.ID;
            var pa = organism.GetThenErasePendingActions();
            if (pa.MoveToAction != null) _moveToActions[id] = pa.MoveToAction;
            if (pa.AttackAction != null) _attackActions[id] = pa.AttackAction;
            if (pa.EatAction != null) _eatActions[id] = pa.EatAction;
            if (pa.ReproduceAction != null) _reproduceActions[id] = pa.ReproduceAction;
            if (pa.DefendAction != null) _defendActions[id] = pa.DefendAction;
        }
    }
}
