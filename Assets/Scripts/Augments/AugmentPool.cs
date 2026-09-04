using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject source of truth for the augment offer pool: the augment entries, the per-rarity
/// offer weights, and the roll (rarity-weighted sampling without replacement). UI just references this
/// and calls Roll();
/// </summary>
[CreateAssetMenu(fileName = "AugmentPool", menuName = "Scriptable Objects/Augment/AugmentPool")]
public class AugmentPool : ScriptableObject
{
    [Tooltip("Every augment eligible to be offered (fill via the context-menu collect in the editor)")]
    [SerializeField] private AugmentData[] entries;

    [Tooltip("Offer weight per rarity. Lower = rarer. Unlisted rarities fall back to weight 1")]
    [SerializeField] private RarityWeight[] rarityWeights =
    {
        new RarityWeight { rarity = AugmentRarity.Common, weight = 10f },
        new RarityWeight { rarity = AugmentRarity.Bronze, weight = 5f },
        new RarityWeight { rarity = AugmentRarity.Silver, weight = 3f },
        new RarityWeight { rarity = AugmentRarity.Gold,   weight = 2f },
    };

    public IReadOnlyList<AugmentData> Entries => entries;

    // Roll //

    /// <summary>
    /// Rarity-weighted sampling without replacement. Skips unique augments already owned. Pass the
    /// player's owned list (or null) so the pool stays decoupled from AugmentManager.
    /// </summary>
    public List<AugmentData> Roll(int count, IReadOnlyList<AugmentData> owned)
    {
        var available = new List<AugmentData>();
        if (entries != null)
            foreach (AugmentData a in entries)
                if (a != null && !(a.unique && Owns(owned, a))) available.Add(a);

        var picked = new List<AugmentData>();
        int want = count < 0 ? available.Count : Mathf.Min(count, available.Count);
        for (int n = 0; n < want; n++)
        {
            float total = 0f;
            foreach (AugmentData a in available) total += WeightOf(a);
            if (total <= 0f) break;

            float r = Random.Range(0f, total);
            int chosen = available.Count - 1;
            for (int i = 0; i < available.Count; i++)
            {
                r -= WeightOf(available[i]);
                if (r <= 0f) { chosen = i; break; }
            }
            picked.Add(available[chosen]);
            available.RemoveAt(chosen); // no duplicate offers
        }
        return picked;
    }

    /// <summary>Offer weight for an augment based on its rarity (falls back to 1 if unmapped).</summary>
    private float WeightOf(AugmentData a)
    {
        if (rarityWeights != null)
            foreach (RarityWeight w in rarityWeights)
                if (w.rarity == a.rarity) return Mathf.Max(0f, w.weight);
        return 1f;
    }

    private static bool Owns(IReadOnlyList<AugmentData> owned, AugmentData a)
    {
        if (owned == null) return false;
        for (int i = 0; i < owned.Count; i++) if (owned[i] == a) return true;
        return false;
    }

    // Editor Auto-Collect //

#if UNITY_EDITOR
    [Header("Editor")]
    [Tooltip("Folders scanned by 'Collect Augments From Folders' (recursive). Add more as new categories appear")]
    [SerializeField] private string[] collectFolders =
    {
        "Assets/Data/Augments/StatAugments",
    };

    /// <summary>Rebuild 'entries' from every AugmentData under the collect folders.</summary>
    [ContextMenu("Collect Augments From Folders")]
    private void Collect()
    {
        var found = new List<AugmentData>();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AugmentData", collectFolders);
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var a = UnityEditor.AssetDatabase.LoadAssetAtPath<AugmentData>(path);
            if (a != null && !found.Contains(a)) found.Add(a);
        }
        found.Sort((x, y) => x.rarity != y.rarity
            ? x.rarity.CompareTo(y.rarity)
            : string.Compare(x.name, y.name, System.StringComparison.Ordinal));
        entries = found.ToArray();
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[AugmentPool] Collected {entries.Length} augments from {collectFolders.Length} folders.");
    }
#endif
}

[System.Serializable]
public struct RarityWeight
{
    public AugmentRarity rarity;
    public float weight;
}
