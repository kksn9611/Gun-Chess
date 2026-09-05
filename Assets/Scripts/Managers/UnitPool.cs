using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared player unit pool. Tracks max capacity and available copies per base champion.
/// Enemy units spawn independently and never touch the pool.
/// </summary>
public class UnitPool : MonoBehaviour
{
    public static UnitPool Instance { get; private set; }

    [Header("Catalog")]
    [SerializeField] private UnitPoolDatabase database;

    [Header("Debug View")]
    public List<PoolDebugInfo> debugPoolState = new List<PoolDebugInfo>();

    [Serializable]
    public struct PoolDebugInfo
    {
        public string unitName;
        public int available;
        public int max;
    }

    // pool debug view
    private void UpdateDebugView()
    {
#if UNITY_EDITOR
        debugPoolState.Clear();
        foreach (var pair in entries)
        {
            debugPoolState.Add(new PoolDebugInfo
            {
                unitName = pair.Key.name,
                available = pair.Value.available,
                max = pair.Value.max
            });
        }
#endif
    }

    // Available/max copies for one base champion
    private class Entry
    {
        public int max;
        public int available;
    }

    // Every UnitData (any star) -> its tier-1 base
    private readonly Dictionary<UnitData, UnitData> baseOf = new();
    // Per-base-champion pool counts
    private readonly Dictionary<UnitData, Entry> entries = new();


    // Events //
    public static event Action<UnitData, int, int> OnPoolChanged; // (baseUnit, available, max)


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        BuildPool();
    }

    /// <summary>Seed the pool from the database, mapping every star variant to its base.</summary>
    private void BuildPool()
    {
        baseOf.Clear();
        entries.Clear();

        if (database == null)
        {
            Debug.LogWarning("[UnitPool] No database assigned");
            return;
        }

        foreach (var baseUnit in database.baseUnits)
        {
            if (baseUnit == null || entries.ContainsKey(baseUnit)) continue;

            int max = database.GetCapacityForCost(baseUnit.cost);
            entries[baseUnit] = new Entry { max = max, available = max };

            // Map base and its whole upgrade chain back to the base
            for (UnitData u = baseUnit; u != null; u = u.upgradeUnit)
            {
                if (!baseOf.ContainsKey(u)) baseOf[u] = baseUnit;
                else break; // guard against shared/cyclic chains
            }
        }
        UpdateDebugView();
    }


    // Queries //

    /// <summary>Max capacity of the unit's base champion.</summary>
    public int GetMax(UnitData unit)
        => ResolveEntry(unit, out Entry e) ? e.max : 0;

    /// <summary>Available copies of the unit's base champion.</summary>
    public int GetAvailable(UnitData unit)
        => ResolveEntry(unit, out Entry e) ? e.available : 0;

    /// <summary>The tier-1 base champion for any star variant (null if not in the database).</summary>
    public UnitData GetBaseUnit(UnitData unit)
        => (unit != null && baseOf.TryGetValue(unit, out UnitData baseUnit)) ? baseUnit : null;


    // Acquire / Return //

    /// <summary>Take copies for this unit's star level from the pool. False if insufficient.</summary>
    public bool TryAcquire(UnitData unit)
    {
        if (!ResolveEntry(unit, out var e)) return false;

        int needed = CopiesFor(unit.starLevel);
        if (e.available < needed) return false;

        e.available -= needed;
        OnPoolChanged?.Invoke(baseOf[unit], e.available, e.max);
        UpdateDebugView();
        return true;
    }

    /// <summary>Return copies for this unit's star level to the pool (clamped to max).</summary>
    public void Return(UnitData unit)
    {
        if (!ResolveEntry(unit, out var e)) return;

        e.available = Mathf.Min(e.max, e.available + CopiesFor(unit.starLevel));
        OnPoolChanged?.Invoke(baseOf[unit], e.available, e.max);
        UpdateDebugView();
    }


    // Roll //

    /// <summary>
    /// Random in-stock base unit of the given cost, weighted by available copies. Bases in
    /// <paramref name="excludedBases"/> (e.g. already maxed to 3-star) are skipped. Null if none
    /// of that cost qualifies. Does not change pool counts.
    /// </summary>
    public UnitData GetRandomAvailableUnit(int targetCost, ICollection<UnitData> excludedBases = null)
    {
        int total = 0;
        foreach (var pair in entries)
            if (Eligible(pair.Key, pair.Value, targetCost, excludedBases))
                total += pair.Value.available;

        if (total == 0) return null;

        int roll = UnityEngine.Random.Range(0, total);
        foreach (var pair in entries)
        {
            if (!Eligible(pair.Key, pair.Value, targetCost, excludedBases)) continue;
            roll -= pair.Value.available;
            if (roll < 0) return pair.Key;
        }
        return null; // unreachable
    }

    private static bool Eligible(UnitData baseUnit, Entry e, int targetCost, ICollection<UnitData> excludedBases)
        => baseUnit.cost == targetCost && e.available > 0
           && (excludedBases == null || !excludedBases.Contains(baseUnit));


    // Helpers //

    /// <summary>Copies a unit of the given star level represents (1★=1, 2★=3, 3★=9).</summary>
    public static int CopiesFor(int starLevel)
    {
        int copies = 1;
        for (int i = 1; i < starLevel; i++) copies *= 3;
        return copies;
    }

    /// <summary>Resolve a unit to its base pool entry. Warns and fails if not in the database.</summary>
    private bool ResolveEntry(UnitData unit, out Entry entry)
    {
        entry = null;
        if (unit == null) return false;

        if (!baseOf.TryGetValue(unit, out UnitData baseUnit))
        {
            Debug.LogWarning($"[UnitPool] Unit '{unit.unitName}' not in pool database");
            return false;
        }
        return entries.TryGetValue(baseUnit, out entry);
    }
}
