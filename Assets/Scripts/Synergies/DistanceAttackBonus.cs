using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Synergy: attack power scales with hex distance to current target.
/// Recomputed each basic attack via UnitController.OnBeforeAttack.
/// </summary>
[CreateAssetMenu(fileName = "DistanceAttackBonus", menuName = "Scriptable Objects/Synergy/DistanceAttackBonus")]
public class DistanceAttackBonus : SynergyBehavior
{
    [Header("Distance Bonus")]
    [Tooltip("Added ATK % per hex of distance to target")]
    public float bonusPerHexPercent = 10f;
    [Tooltip("Cap on total bonus %")]
    public float maxBonusPercent = 200f;
    [Tooltip("Minimum hex distance for the bonus to apply (below this = no bonus)")]
    public int minDistance = 3;

    // Per-unit cached bonus % (the SO is shared across all units)
    private readonly Dictionary<UnitController, float> lastApplied = new Dictionary<UnitController, float>();

    private void OnEnable()
    {
        lastApplied.Clear();
    }
    public override void Apply(UnitController unit)
    {
        lastApplied[unit] = 0f;
        unit.OnBeforeAttack += UpdateAttack;
    }

    public override void Remove(UnitController unit)
    {
        unit.OnBeforeAttack -= UpdateAttack;
        if (lastApplied.TryGetValue(unit, out float prev) && prev != 0f)
            unit.Stats.ApplyStatModifier(StatType.Att, -prev);
        lastApplied.Remove(unit);
    }

    // Distance Bonus Update //

    private void UpdateAttack(UnitController attacker, UnitController target)
    {
        if (target == null) return;

        int dist = HexCoordCal.GetDistance(attacker.CurrentCoord, target.CurrentCoord);
        float desired = (dist >= minDistance)
            ? Mathf.Min(dist * bonusPerHexPercent, maxBonusPercent)
            : 0f;
        float prev = lastApplied.TryGetValue(attacker, out var p) ? p : 0f;
        float delta = desired - prev;

        if (!Mathf.Approximately(delta, 0f))
        {
            attacker.Stats.ApplyStatModifier(StatType.Att, delta);
            lastApplied[attacker] = desired;
        }
    }
}
