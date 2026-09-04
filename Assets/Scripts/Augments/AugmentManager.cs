using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the player's chosen augments (the source of truth). Application of their stat buffs is delegated
/// to the reconciled player-board rebuild (SynergyManager) via the aggregate + OnAugmentsChanged, so
/// merges/placements/round-resets are handled by the existing machinery
/// </summary>
public class AugmentManager : MonoBehaviour
{
    public static AugmentManager Instance { get; private set; }

    private readonly List<AugmentData> owned = new List<AugmentData>();
    public IReadOnlyList<AugmentData> Owned => owned;

    /// <summary>Fires whenever the owned set changes (drives a board-buff rebuild).</summary>
    public static event Action OnAugmentsChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Add an augment (running its one-shot effects). Unique augments can't be added twice.</summary>
    public bool Choose(AugmentData augment)
    {
        if (augment == null) return false;
        if (augment.unique && owned.Contains(augment)) return false;

        owned.Add(augment);
        if (augment.effects != null)
            foreach (AugmentEffect e in augment.effects)
                if (e != null) e.OnAcquire();

        OnAugmentsChanged?.Invoke();
        return true;
    }

    /// <summary>Clear all owned augments (e.g. new game).</summary>
    public void ClearAll()
    {
        if (owned.Count == 0) return;
        owned.Clear();
        OnAugmentsChanged?.Invoke();
    }

    /// <summary>Summed stat boosts across every owned augment (stacking is additive per StatType).</summary>
    public List<StatBoostEntry> AggregateBoosts()
    {
        var sum = new Dictionary<StatType, float>();
        foreach (AugmentData a in owned)
        {
            if (a == null || a.effects == null) continue;
            foreach (AugmentEffect e in a.effects)
            {
                if (e is StatAugmentEffect s && s.boosts != null)
                    foreach (StatBoostEntry b in s.boosts)
                        sum[b.statType] = (sum.TryGetValue(b.statType, out float v) ? v : 0f) + b.percentBoost;
            }
        }

        var list = new List<StatBoostEntry>(sum.Count);
        foreach (var kv in sum) list.Add(new StatBoostEntry { statType = kv.Key, percentBoost = kv.Value });
        return list;
    }

    /// <summary>Summed bonus synergy counts across every owned augment (keyed by synergy).</summary>
    public Dictionary<SynergyData, int> AggregateSynergyBonuses()
    {
        var sum = new Dictionary<SynergyData, int>();
        foreach (AugmentData a in owned)
        {
            if (a == null || a.effects == null) continue;
            foreach (AugmentEffect e in a.effects)
            {
                if (e is SynergyCountAugmentEffect s && s.targetSynergy != null && s.bonusCount != 0)
                    sum[s.targetSynergy] = (sum.TryGetValue(s.targetSynergy, out int v) ? v : 0) + s.bonusCount;
            }
        }
        return sum;
    }
}
