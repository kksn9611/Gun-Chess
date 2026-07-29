using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the board placement limit (max field units). Capacity = base(level) + synergy bonus
/// + generic modifiers. Computation is centralized here; producers and enforcement hook in later.
/// </summary>
public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    [Header("Base Capacity")]
    [Tooltip("Field-unit capacity per player level (level = index + 1)")]
    private int[] capacityPerLevel = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    [Header("References")]
    [SerializeField] private SynergyManager synergyManager; // channel A source (synergy bonus)
    [SerializeField] private SynergyState synergyState;     // fires OnSynergyChanged when the bonus changes

    // Channel B: generic modifiers keyed by source (items, augments, buffs). Idempotent per source.
    private readonly Dictionary<Object, int> modifiers = new Dictionary<Object, int>();

    /// <summary>Fires with the new capacity whenever it changes (level, synergy, or modifier).</summary>
    public static event System.Action<int> OnCapacityChanged;

    private bool enforcing; // reentrancy guard: benching fires events that re-enter EnforceCapacity

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void OnEnable()
    {
        PlayerManager.OnLevelChanged += OnLevelChanged;               // base capacity depends on level
        if (synergyState != null) synergyState.OnSynergyChanged += OnCapacityDirty; // channel A
    }

    private void OnDisable()
    {
        PlayerManager.OnLevelChanged -= OnLevelChanged;
        if (synergyState != null) synergyState.OnSynergyChanged -= OnCapacityDirty;
    }

    private void OnLevelChanged(int _) => OnCapacityDirty();

    /// <summary>A capacity input changed: bench any over-cap excess, then broadcast.</summary>
    private void OnCapacityDirty()
    {
        EnforceCapacity();
        OnCapacityChanged?.Invoke(Capacity);
    }


    // Capacity //

    /// <summary>Current max field units: base(level) + synergy bonus + generic modifiers.</summary>
    public int Capacity => BaseCapacity() + SynergyBonus() + ModifierSum();

    /// <summary>Units currently on the field (bench excluded). Prep-time board count.</summary>
    public int FieldCount => UnitManager.Instance != null ? UnitManager.Instance.playerUnits.Count : 0;

    /// <summary>True while another unit may be placed on the field.</summary>
    public bool HasRoom => FieldCount < Capacity;

    /// <summary>Base capacity from the player's level (clamped to the table).</summary>
    private int BaseCapacity()
    {
        int level = PlayerManager.Instance != null ? PlayerManager.Instance.CurrentLevel : 1;
        if (capacityPerLevel == null || capacityPerLevel.Length == 0) return level;
        int idx = Mathf.Clamp(level - 1, 0, capacityPerLevel.Length - 1);
        return capacityPerLevel[idx];
    }

    // Channel A : synergy-driven bonus.
    private int SynergyBonus() => synergyManager != null ? synergyManager.CalculateBoardBonus() : 0;

    private int ModifierSum()
    {
        int total = 0;
        foreach (var kv in modifiers) total += kv.Value;
        return total;
    }


    // Channel B: modifiers //

    /// <summary>Set (or replace) this source's capacity delta (e.g. items, augments, buffs).</summary>
    public void SetModifier(Object source, int delta)
    {
        if (source == null) return;
        modifiers[source] = delta;
        OnCapacityDirty();
    }

    /// <summary>Remove this source's capacity delta.</summary>
    public void ClearModifier(Object source)
    {
        if (source != null && modifiers.Remove(source))
            OnCapacityDirty();
    }


    // Enforcement (policy A: auto-bench newest) //

    /// <summary>
    /// Move the newest field units to the bench until FieldCount fits Capacity.
    /// Stops early if the bench is full (over-cap then caught by the battle-start guard).
    /// </summary>
    public void EnforceCapacity()
    {
        if (enforcing) return; // benching fires OnBenchState -> synergy recalc -> re-entry
        enforcing = true;
        try
        {
            var players = UnitManager.Instance.playerUnits;
            while (players.Count > Capacity)
            {
                UnitController unit = players[players.Count - 1]; // newest placed
                BenchTileScript slot = BenchManager.Instance.GetEmptySlot();
                if (slot == null) break; // bench full — leave excess for the battle-start guard

                UnitManager.Instance.RemoveUnit(unit, unit.CurrentTeam);
                BenchManager.Instance.AddUnit(unit, slot);
                unit.PlaceOnBench(slot);
            }
        }
        finally { enforcing = false; }
    }
}
