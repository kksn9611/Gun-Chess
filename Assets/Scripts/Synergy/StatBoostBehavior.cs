using UnityEngine;

/// <summary>
/// Synergy effect asset that modifies multiple stats by percentage.
/// Configure stat types and boost percentages in Inspector.
/// </summary>
[CreateAssetMenu(fileName = "StatBoost", menuName = "Scriptable Objects/Synergy/StatBoostBehavior")]
public class StatBoostBehavior : SynergyBehavior
{
    [Tooltip("Stat boosts to apply")]
    public StatBoostEntry[] boosts;

    public override void Apply(UnitController unit)
    {
        if (boosts == null) return;
        foreach (var entry in boosts)
            unit.Stats.ApplyStatModifier(entry.statType, entry.percentBoost);
    }

    public override void Remove(UnitController unit)
    {
        if (boosts == null) return;
        foreach (var entry in boosts)
            unit.Stats.ApplyStatModifier(entry.statType, -entry.percentBoost);
    }
}

/// <summary>
/// A single stat boost entry. Defines stat type and boost percentage.
/// </summary>
[System.Serializable]
public struct StatBoostEntry
{
    [Tooltip("Stat type to modify")]
    public StatType statType;

    [Tooltip("Boost percentage (%). e.g., 20 = +20%")]
    public float percentBoost;
}
