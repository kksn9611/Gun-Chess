using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Anti-stall reaction: applies stat boosts to all active board units on each trigger (stacks
/// additively). Round-end ResetStats reverts every accumulated stack automatically.
/// </summary>
public class AntiStallBuffReactor : MonoBehaviour
{
    [Tooltip("Boosts applied to every active board unit per trigger")]
    [SerializeField] private StatBoostEntry[] perStackBoosts;
    [SerializeField] private TargetSide side = TargetSide.All;

    private void OnEnable()  => AntiStallController.OnTriggered += Apply;
    private void OnDisable() => AntiStallController.OnTriggered -= Apply;

    private void Apply(int stack)
    {
        if (perStackBoosts == null || UnitManager.Instance == null) return;

        foreach (UnitController u in Targets())
        {
            if (u == null || u.Stats == null || u.Stats.CurrentHp <= 0f) continue; // active only
            foreach (StatBoostEntry b in perStackBoosts)
                u.Stats.ApplyStatModifier(b.statType, b.percentBoost);
        }
    }

    /// <summary>Live board units for the configured side (bench excluded — they aren't in UnitManager).</summary>
    private IEnumerable<UnitController> Targets()
    {
        UnitManager um = UnitManager.Instance;
        if (side != TargetSide.Enemy)
            foreach (UnitController u in um.playerUnits) yield return u;
        if (side != TargetSide.Player)
            foreach (UnitController u in um.enemyUnits) yield return u;
    }
}
