using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tallies synergies for player units on the field
/// and records active state in SynergyState SO.
/// </summary>
public class SynergyManager : MonoBehaviour
{
    public static SynergyManager Instance { get; private set; }

    [Header("Shared Data")]
    [Tooltip("Project-wide shared SynergyState asset")]
    [SerializeField] private SynergyState synergyState;

    /// <summary>
    /// Clear stale synergy state at game start. SynergyState is a shared ScriptableObject, so its
    /// entries persist between play sessions and would otherwise carry over from a previous run.
    /// </summary>
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        ResetState();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Clear the shared synergy state.</summary>
    public void ResetState()
    {
        if (synergyState != null) synergyState.Clear();
    }

    /// <summary>
    /// Subscribe to OnBenchState on unit spawn
    /// to recalculate synergies on field↔bench transitions.
    /// </summary>
    private void OnEnable()
    {
        UnitController.OnUnitSpawned += OnUnitSpawned;
        BattleManager.OnBattleEnd   += OnBattleEnd;
        AugmentManager.OnAugmentsChanged += Recalculate; // re-apply player buffs when augments change
    }

    private void OnDisable()
    {
        UnitController.OnUnitSpawned -= OnUnitSpawned;
        BattleManager.OnBattleEnd   -= OnBattleEnd;
        AugmentManager.OnAugmentsChanged -= Recalculate;
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

        // 0) Strip every board unit's synergy buffs first. The re-apply below (UpdateEntries →
        //    OnSynergyChanged) then rebuilds them over the CURRENT roster, so a unit placed or merged
        //    onto the board after a global boost was applied still receives it (fixes the late-joiner
        //    bug where e.g. a merged Star2 missed a team-wide GlobalStatBoost).
        foreach (var unit in UnitManager.Instance.playerUnits)
            if (unit != null && !unit.IsOnBench)
                unit.Stats.RemoveAllSynergyBuffs();

        // 1) Tally field unit synergies (same champion counted once across star tiers)
        Dictionary<SynergyData, int> synergyCounts = new Dictionary<SynergyData, int>();
        HashSet<string> countedUnits = new HashSet<string>();

        foreach (var unit in UnitManager.Instance.playerUnits)
        {
            if (unit == null || unit.IsOnBench) continue;
            if (unit.Stats.UnitData == null || unit.Stats.UnitData.synergies == null) continue;

            // Count each champion once by unitName — star tiers share a name, so duplicates don't stack
            if (!countedUnits.Add(unit.Stats.UnitData.unitName)) continue;

            foreach (var synergy in unit.Stats.UnitData.synergies)
            {
                if (synergy == null) continue;
                if (synergyCounts.ContainsKey(synergy))
                    synergyCounts[synergy]++;
                else
                    synergyCounts[synergy] = 1;
            }
        }

        // 1b) Fold in augment synergy-count bonuses (can activate a synergy from zero units)
        if (AugmentManager.Instance != null)
            foreach (var bonus in AugmentManager.Instance.AggregateSynergyBonuses())
            {
                if (bonus.Key == null) continue;
                synergyCounts[bonus.Key] = (synergyCounts.TryGetValue(bonus.Key, out int c) ? c : 0) + bonus.Value;
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

        // 4) Apply augment stat buffs over the current player board (idempotent per-unit reconcile, so
        //    late/merged units get them and repeated rebuilds don't stack).
        var augBoosts = AugmentManager.Instance != null ? AugmentManager.Instance.AggregateBoosts() : null;
        foreach (var unit in UnitManager.Instance.playerUnits)
            if (unit != null && !unit.IsOnBench)
                unit.Stats.SetAugmentBoosts(augBoosts);

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
    /// Total gold granted by active synergy tiers this round. Calc-only (no side effects),
    /// so UI can preview the same value RoundManager grants.
    /// </summary>
    public int CalculateRoundIncome()
    {
        if (synergyState == null) return 0;

        int total = 0;
        foreach (var entry in synergyState.Entries)
        {
            if (entry.activeTierIndex < 0 || entry.synergy == null) continue;
            var behaviors = entry.synergy.tiers[entry.activeTierIndex].behaviors;
            if (behaviors == null) continue;
            foreach (var b in behaviors)
                if (b is GoldPerRoundBehavior gold) total += gold.goldPerRound;
        }
        return total;
    }

    /// <summary>
    /// Total bonus board slots from active synergy tiers. Calc-only (no side effects),
    /// so BoardManager can fold it into the placement limit.
    /// </summary>
    public int CalculateBoardBonus()
    {
        if (synergyState == null) return 0;

        int total = 0;
        foreach (var entry in synergyState.Entries)
        {
            if (entry.activeTierIndex < 0 || entry.synergy == null) continue;
            var behaviors = entry.synergy.tiers[entry.activeTierIndex].behaviors;
            if (behaviors == null) continue;
            foreach (var b in behaviors)
                if (b is BoardCapacityBehavior cap) total += cap.bonusSlots;
        }
        return total;
    }

    /// <summary>
    /// Round Income Synergy (called by RoundManager). Grants CalculateRoundIncome() to the player.
    /// </summary>
    public void GrantRoundIncome()
    {
        int income = CalculateRoundIncome();
        if (income > 0 && PlayerManager.Instance != null) PlayerManager.Instance.AddGold(income);
    }
}
