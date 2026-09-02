using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays active synergies as a paged list of badges on the left of the screen.
/// Subscribes to SynergyState's OnSynergyChanged for auto-refresh. Sorting is manual
/// (per-tier Inspector keys); inactive synergies are always shown.
/// </summary>
public class SynergyUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Shared synergy state data")]
    [SerializeField] private SynergyState synergyState;

    [Tooltip("Badge row prefab (SynergyBadgeUI)")]
    [SerializeField] private SynergyBadgeUI badgePrefab;

    [Tooltip("Parent with a VerticalLayoutGroup that holds the badge rows")]
    [SerializeField] private RectTransform listContainer;

    [Tooltip("Shared hover tooltip injected into each badge")]
    [SerializeField] private SynergyTooltip tooltip;

    [Header("Paging")]
    [Tooltip("Button that advances to the next page (hidden when only one page)")]
    [SerializeField] private Button pageButton;
    [Tooltip("Optional label, e.g. \"1/3\"")]
    [SerializeField] private TextMeshProUGUI pageLabel;
    [Tooltip("Badges shown per page")]
    [Min(1)]
    [SerializeField] private int pageSize = 6;

    [Header("Inactive Sort")]
    [Tooltip("Sort primary applied to any inactive synergy (sinks it to the bottom)")]
    [SerializeField] private int inactiveSortPrimary = 99;
    [Tooltip("Sort secondary for inactive weapon synergies (below inactive class synergies)")]
    [SerializeField] private int inactiveWeaponSecondary = 1;

    private readonly List<SynergyBadgeUI> pool = new List<SynergyBadgeUI>();
    private int page;

    private void OnEnable()
    {
        if (synergyState != null) synergyState.OnSynergyChanged += Render;
        if (pageButton   != null) pageButton.onClick.AddListener(NextPage);
        Render();
    }

    private void OnDisable()
    {
        if (synergyState != null) synergyState.OnSynergyChanged -= Render;
        if (pageButton   != null) pageButton.onClick.RemoveListener(NextPage);
    }

    // Paging //

    private void NextPage()
    {
        int pages = PageCount();
        if (pages <= 1) return;
        page = (page + 1) % pages;
        Render();
    }

    private int PageCount()
    {
        int count = synergyState != null ? synergyState.Entries.Count : 0;
        return Mathf.Max(1, Mathf.CeilToInt(count / (float)pageSize));
    }

    // Render //

    /// <summary>Sort, slice to the current page, and bind the pooled badge rows.</summary>
    private void Render()
    {
        if (synergyState == null || badgePrefab == null || listContainer == null) return;

        List<SynergyEntry> sorted = SortedEntries();

        int pages = PageCount();
        page = Mathf.Clamp(page, 0, pages - 1); // keep page valid as the list changes

        int start = page * pageSize;
        int end   = Mathf.Min(start + pageSize, sorted.Count);

        // Bind the visible slice into pooled rows (grows the pool on demand)
        int shown = 0;
        for (int i = start; i < end; i++, shown++)
            GetRow(shown).Bind(sorted[i]);

        // Hide any leftover pooled rows
        for (int i = shown; i < pool.Count; i++)
            pool[i].Hide();

        // Page button + label
        if (pageButton != null) pageButton.gameObject.SetActive(pages > 1);
        if (pageLabel  != null) pageLabel.text = pages > 1 ? $"{page + 1}/{pages}" : "";
    }

    /// <summary>Get (or lazily create) the pooled badge row at index.</summary>
    private SynergyBadgeUI GetRow(int index)
    {
        while (pool.Count <= index)
        {
            SynergyBadgeUI row = Instantiate(badgePrefab, listContainer);
            row.SetTooltip(tooltip);
            pool.Add(row);
        }
        SynergyBadgeUI r = pool[index];
        r.transform.SetSiblingIndex(index); // keep display order == sort order
        return r;
    }

    // Sorting (manual per-tier keys) //

    /// <summary>
    /// Entries ordered by: sortPrimary asc, then deployed unit count desc (more units first) as the
    /// tie-breaker, then sortSecondary asc as the final tie-breaker (lower shown first).
    /// </summary>
    private List<SynergyEntry> SortedEntries()
    {
        var sorted = new List<SynergyEntry>(synergyState.Entries);
        sorted.Sort((a, b) =>
        {
            GetSortKeys(a, out int ap, out int asec);
            GetSortKeys(b, out int bp, out int bsec);
            int byPrimary = ap.CompareTo(bp);
            if (byPrimary != 0) return byPrimary;
            // Tie-breaker: more deployed units on the board shown first.
            int byCount = b.currentCount.CompareTo(a.currentCount);
            if (byCount != 0) return byCount;
            // Final tie-breaker: existing per-tier sort secondary (ascending).
            return asec.CompareTo(bsec);
        });
        return sorted;
    }

    /// <summary>
    /// Sort keys for an entry. Inactive synergies get a fixed low priority (weapons below classes),
    /// independent of the per-tier keys. Active synergies use their active tier's manual keys.
    /// </summary>
    private void GetSortKeys(SynergyEntry entry, out int primary, out int secondary)
    {
        primary = 0; secondary = 0;
        var synergy = entry.synergy;
        if (synergy == null) return;

        // Inactive: fixed sink-to-bottom priority, weapons after classes.
        if (entry.activeTierIndex < 0)
        {
            primary   = inactiveSortPrimary;
            secondary = synergy.isWeapon ? inactiveWeaponSecondary : 0;
            return;
        }

        // Active: use the active tier's manual sort keys.
        if (synergy.tiers == null || synergy.tiers.Length == 0) return;
        int idx = Mathf.Min(entry.activeTierIndex, synergy.tiers.Length - 1);
        primary   = synergy.tiers[idx].sortPrimary;
        secondary = synergy.tiers[idx].sortSecondary;
    }
}
