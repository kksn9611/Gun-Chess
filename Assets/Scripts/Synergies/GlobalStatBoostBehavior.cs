using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Synergy effect that applies stat boosts to ALL allied units. Tracks the exact set of boosted units
/// so it reconciles correctly when the roster changes (e.g. a merged unit joins mid-round): Apply boosts
/// any current ally not already boosted; Remove strips exactly the boosted units (never a unit that never
/// received it, which would push it below base).
/// </summary>
[CreateAssetMenu(fileName = "GlobalStatBoost", menuName = "Scriptable Objects/Synergy/GlobalStatBoostBehavior")]
public class GlobalStatBoostBehavior : SynergyBehavior
{
    [Tooltip("Stat boosts to apply to all allies")]
    public StatBoostEntry[] boosts;

    private readonly HashSet<UnitController> boosted = new HashSet<UnitController>();

    public override void Apply(UnitController unit)
    {
        if (boosts == null) return;

        // Reconcile: boost every current ally not already boosted (idempotent — catches late joiners).
        IReadOnlyList<UnitController> allies = UnitManager.Instance.GetAlliesOf(unit.CurrentTeam);
        foreach (UnitController ally in allies)
        {
            if (ally == null || !boosted.Add(ally)) continue; // Add returns false if already boosted
            foreach (StatBoostEntry entry in boosts)
                ally.Stats.ApplyStatModifier(entry.statType, entry.percentBoost);
        }
    }

    public override void Remove(UnitController unit)
    {
        if (boosts == null || boosted.Count == 0) return;

        // Strip exactly the units we boosted (not the whole roster) so a never-boosted joiner isn't
        // pushed below base.
        foreach (UnitController ally in boosted)
        {
            if (ally == null) continue;
            foreach (StatBoostEntry entry in boosts)
                ally.Stats.ApplyStatModifier(entry.statType, -entry.percentBoost);
        }
        boosted.Clear();
    }
}
