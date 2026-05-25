using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Synergy effect that applies stat boosts to ALL allied units.
/// Uses a HashSet to ensure the effect is applied/removed only once per team.
/// </summary>
[CreateAssetMenu(fileName = "GlobalStatBoost", menuName = "Scriptable Objects/Synergy/GlobalStatBoostBehavior")]
public class GlobalStatBoostBehavior : SynergyBehavior
{
    [Tooltip("Stat boosts to apply to all allies")]
    public StatBoostEntry[] boosts;

    private readonly HashSet<Team> appliedTeams = new HashSet<Team>();

    public override void Apply(UnitController unit)
    {
        if (boosts == null) return;

        Team team = unit.CurrentTeam;
        if (appliedTeams.Contains(team)) return; // already applied this cycle
        appliedTeams.Add(team);

        IReadOnlyList<UnitController> allies = UnitManager.Instance.GetAlliesOf(team);
        foreach (UnitController ally in allies)
        {
            if (ally == null) continue;
            foreach (StatBoostEntry entry in boosts)
                ally.Stats.ApplyStatModifier(entry.statType, entry.percentBoost);
        }
    }

    public override void Remove(UnitController unit)
    {
        if (boosts == null) return;

        Team team = unit.CurrentTeam;
        if (!appliedTeams.Contains(team)) return; // already removed
        appliedTeams.Remove(team);

        IReadOnlyList<UnitController> allies = UnitManager.Instance.GetAlliesOf(team);
        foreach (UnitController ally in allies)
        {
            if (ally == null) continue;
            foreach (StatBoostEntry entry in boosts)
                ally.Stats.ApplyStatModifier(entry.statType, -entry.percentBoost);
        }
    }
}
