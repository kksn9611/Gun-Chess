using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Displays active synergy list and tiers as text on the left side of screen.
/// Subscribes to SynergyState SO's OnSynergyChanged event for auto-refresh.
/// </summary>
public class SynergyUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Shared synergy state data")]
    [SerializeField] private SynergyState synergyState;

    [Tooltip("TMP text to display synergy info")]
    [SerializeField] private TextMeshProUGUI synergyText;

    private void OnEnable()
    {
        if (synergyState != null)
            synergyState.OnSynergyChanged += UpdateUI;
    }

    private void OnDisable()
    {
        if (synergyState != null)
            synergyState.OnSynergyChanged -= UpdateUI;
    }

    /// <summary>
    /// Called when synergy state changes. Display active synergies as text.
    /// </summary>
    private void UpdateUI()
    {
        if (synergyText == null || synergyState == null) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>[ Synergy ]</b>");
        sb.AppendLine();

        foreach (var entry in SortedEntries())
        {
            if (entry.synergy == null) continue;

            // Color based on active state
            string color = entry.activeTierIndex >= 0 ? "#FFD700" : "#888888";
            string tierLabel = entry.activeTierIndex >= 0
                ? $"Tier {entry.activeTierIndex + 1}"
                : "-";

            // Show tier thresholds (e.g., 2/4/6)
            string thresholds = "";
            if (entry.synergy.tiers != null && entry.synergy.tiers.Length > 0)
            {
                var parts = new string[entry.synergy.tiers.Length];
                for (int i = 0; i < entry.synergy.tiers.Length; i++)
                {
                    // Highlight current active tier
                    if (i == entry.activeTierIndex)
                        parts[i] = $"<b>{entry.synergy.tiers[i].requiredCount}</b>";
                    else
                        parts[i] = $"{entry.synergy.tiers[i].requiredCount}";
                }
                thresholds = $" ({string.Join("/", parts)})";
            }

            sb.AppendLine($"<color={color}>{entry.synergy.synergyName}</color>");
            sb.AppendLine($"  {entry.currentCount} units | {tierLabel}{thresholds}");
        }

        // No synergies placed
        if (synergyState.Entries.Count == 0)
            sb.AppendLine("<color=#888888>No synergies placed</color>");

        synergyText.text = sb.ToString();
    }

    // Sorting //

    /// <summary>
    /// Entries ordered for display by the active tier's Inspector sort keys:
    /// sortPrimary asc, then sortSecondary asc as tiebreak (lower shown first).
    /// </summary>
    private List<SynergyEntry> SortedEntries()
    {
        var sorted = new List<SynergyEntry>(synergyState.Entries);
        sorted.Sort((a, b) =>
        {
            GetSortKeys(a, out int ap, out int asec);
            GetSortKeys(b, out int bp, out int bsec);
            int byPrimary = ap.CompareTo(bp); // primary key
            if (byPrimary != 0) return byPrimary;
            return asec.CompareTo(bsec);       // tiebreak
        });
        return sorted;
    }

    /// <summary>Sort keys from the entry's active tier (falls back to tier 0 while inactive).</summary>
    private static void GetSortKeys(SynergyEntry entry, out int primary, out int secondary)
    {
        primary = 0; secondary = 0;
        var synergy = entry.synergy;
        if (synergy == null || synergy.tiers == null || synergy.tiers.Length == 0) return;

        int idx = entry.activeTierIndex >= 0
            ? Mathf.Min(entry.activeTierIndex, synergy.tiers.Length - 1)
            : 0; // inactive: use the first tier's keys
        primary   = synergy.tiers[idx].sortPrimary;
        secondary = synergy.tiers[idx].sortSecondary;
    }
}
