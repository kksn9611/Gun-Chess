using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tallies synergies for player units on the field
/// and records active state in SynergyState SO.
/// </summary>
public class SynergyManager : MonoBehaviour
{
    [Header("Shared Data")]
    [Tooltip("Project-wide shared SynergyState asset")]
    [SerializeField] private SynergyState synergyState;

    /// <summary>
    /// Subscribe to OnBenchState on unit spawn
    /// to recalculate synergies on field↔bench transitions.
    /// </summary>
    private void OnEnable()
    {
        UnitController.OnUnitSpawned += OnUnitSpawned;
        BattleManager.OnBattleEnd   += OnBattleEnd;
    }

    private void OnDisable()
    {
        UnitController.OnUnitSpawned -= OnUnitSpawned;
        BattleManager.OnBattleEnd   -= OnBattleEnd;
    }

    /// <summary>
    /// Subscribe to OnBenchState when a unit spawns.
    /// Only player units affect synergies.
    /// </summary>
    private void OnUnitSpawned(UnitController unit)
    {
        if (unit.CurrentTeam != Team.Player) return;
        unit.OnBenchState += _ => Recalculate();
        // Recalculate on spawn too (unit may spawn directly on field)
        Recalculate();
    }

    /// <summary>
    /// Recalculate synergies on battle end.
    /// RestorePlayerPositions() triggers PlaceOnTile() → OnBenchState → Recalculate(),
    /// but we also recalculate after full restore to guarantee final state.
    /// No Clear() needed — Recalculate() overwrites the entire state each time.
    /// </summary>
    private void OnBattleEnd(Team winner)
    {
        // Recalculate auto-triggers via OnBenchState after RestorePlayerPositions()
    }

    /// <summary>
    /// Tally synergy tags from field player units (excluding bench)
    /// and update SynergyState by comparing against SynergyData tier thresholds.
    /// </summary>
    public void Recalculate()
    {
        if (synergyState == null) return;

        // 1) Tally field unit synergies (same UnitData counted once)
        Dictionary<SynergyData, int> synergyCounts = new Dictionary<SynergyData, int>();
        HashSet<UnitData> countedUnitData = new HashSet<UnitData>();

        foreach (var unit in UnitManager.Instance.playerUnits)
        {
            if (unit == null || unit.IsOnBench) continue;
            if (unit.Stats.UnitData == null || unit.Stats.UnitData.synergies == null) continue;

            // Count each UnitData only once even if multiple units share it
            if (!countedUnitData.Add(unit.Stats.UnitData)) continue;

            foreach (var synergy in unit.Stats.UnitData.synergies)
            {
                if (synergy == null) continue;
                if (synergyCounts.ContainsKey(synergy))
                    synergyCounts[synergy]++;
                else
                    synergyCounts[synergy] = 1;
            }
        }

        // 2) Build SynergyEntry list
        List<SynergyEntry> newEntries = new List<SynergyEntry>();

        foreach (var pair in synergyCounts)
        {
            SynergyData synergy = pair.Key;
            int count = pair.Value;
            int tierIndex = synergy.GetActiveTierIndex(count);

            newEntries.Add(new SynergyEntry
            {
                synergy = synergy,
                currentCount = count,
                activeTierIndex = tierIndex
            });
        }

        // 3) Write to SynergyState → fire OnSynergyChanged
        synergyState.UpdateEntries(newEntries);

        // Debug log
        foreach (var entry in newEntries)
        {
            if (entry.activeTierIndex >= 0)
            {
                Debug.Log($"[Synergy] {entry.synergy.synergyName}: " +
                          $"{entry.currentCount} units → Tier {entry.activeTierIndex + 1} active");
            }
        }
    }
    /// <summary>
    /// Round Income Synergy (called by RoundManager)
    /// </summary>
    public void GrantRoundIncome()
    {
        if (synergyState == null) return;
        foreach (var entry in synergyState.Entries)
        {
            if (entry.activeTierIndex < 0 || entry.synergy == null) continue;
            var behaviors = entry.synergy.tiers[entry.activeTierIndex].behaviors;
            if (behaviors == null) continue;
            foreach (var b in behaviors)
                if (b is GoldPerRoundBehavior gold) gold.GrantIncome();
        }
    }
}
