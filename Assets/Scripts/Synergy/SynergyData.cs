using UnityEngine;

/// <summary>
/// ScriptableObject containing synergy name, description, tier conditions and effects.
/// </summary>
[CreateAssetMenu(fileName = "SynergyData", menuName = "Scriptable Objects/SynergyData")]
public class SynergyData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Synergy name")]
    public string synergyName = "New Synergy";

    [Header("Description")]
    [TextArea(2, 4)]
    public string description = "";

    [Header("Tier Settings")]
    [Tooltip("Tier array. Set requiredCount in ascending order.")]
    public SynergyTier[] tiers;

    /// <summary>
    /// Return active tier index for given unit count.
    /// </summary>
    public int GetActiveTierIndex(int unitCount)
    {
        int activeIndex = -1;
        for (int i = 0; i < tiers.Length; i++)
        {
            if (unitCount >= tiers[i].requiredCount)
                activeIndex = i;
            else
                break; // Ascending order — all subsequent tiers unmet
        }
        return activeIndex;
    }
}

/// <summary>
/// Synergy tier configuration.
/// </summary>
[System.Serializable]
public struct SynergyTier
{
    [Tooltip("Required synergy unit count")]
    public int requiredCount;

    [Tooltip("Effect assets applied at this tier (StatBoost, special effects, etc.)")]
    public SynergyBehavior[] behaviors;
}
