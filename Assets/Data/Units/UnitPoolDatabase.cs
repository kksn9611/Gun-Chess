using UnityEngine;

/// <summary>
/// Catalog of player-rollable base units and the per-cost pool capacity table.
/// Lists tier-1 base units only; star variants are resolved at runtime.
/// </summary>
[CreateAssetMenu(fileName = "UnitPoolDatabase", menuName = "Scriptable Objects/UnitPoolDatabase")]
public class UnitPoolDatabase : ScriptableObject
{
    [Tooltip("Tier-1 base champions only. upgradeUnit chains are walked at runtime.")]
    public UnitData[] baseUnits;

    [Tooltip("Pool copies available per cost tier (TFT-style)")]
    public CostCapacity[] costTable;

    /// <summary>Copies available for a given cost tier; 0 (with warning) if unlisted.</summary>
    public int GetCapacityForCost(int cost)
    {
        foreach (var entry in costTable)
            if (entry.cost == cost) return entry.copies;

        Debug.LogWarning($"[UnitPoolDatabase] No capacity entry for cost {cost}");
        return 0;
    }
}

/// <summary>Pool capacity for a single cost tier.</summary>
[System.Serializable]
public struct CostCapacity
{
    public int cost;    // Unit cost tier
    public int copies;  // Max copies in the pool
}
