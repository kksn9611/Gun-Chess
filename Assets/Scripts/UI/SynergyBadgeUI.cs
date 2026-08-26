using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// One synergy row in the left panel: tier badge on the left, name + count stacked on the right.
/// Bound by SynergyUI. Dims while the synergy is inactive.
/// </summary>
public class SynergyBadgeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Image badge;                // tier icon (active) / inactive icon
    [SerializeField] private GameObject largeCountGroup; // bordered box around the big count (toggled active); falls back to largeCountText's object
    [SerializeField] private TextMeshProUGUI largeCountText; // big unit count next to the badge (active only)
    [SerializeField] private TextMeshProUGUI nameText;   // synergy name (top line)
    [SerializeField] private TextMeshProUGUI countText;  // breakpoints (active) / "current/next" (inactive)
    [SerializeField] private CanvasGroup canvasGroup;    // dim when inactive

    [Header("Style")]
    [Range(0f, 1f)]
    [SerializeField] private float inactiveAlpha = 0.5f;
    [Tooltip("Color of the non-current breakpoint numbers")]
    [SerializeField] private string dimHex = "#8A8A8A";

    private SynergyTooltip tooltip;   // shared tooltip, injected by SynergyUI
    private SynergyEntry currentEntry; // last bound entry (for hover)
    private bool hasEntry;

    /// <summary>Inject the shared tooltip used on hover.</summary>
    public void SetTooltip(SynergyTooltip t) => tooltip = t;

    /// <summary>Show this entry, picking the active tier's badge and stacking name over count.</summary>
    public void Bind(SynergyEntry entry)
    {
        currentEntry = entry;
        hasEntry = entry.synergy != null;

        SynergyData synergy = entry.synergy;
        if (synergy == null) { gameObject.SetActive(false); return; }
        gameObject.SetActive(true);

        bool active = entry.activeTierIndex >= 0;

        // Badge: active tier icon → generic/inactive fallback
        Sprite sprite = null;
        if (active && synergy.tiers != null && entry.activeTierIndex < synergy.tiers.Length)
            sprite = synergy.tiers[entry.activeTierIndex].icon;
        if (sprite == null) sprite = active ? synergy.icon : synergy.inactiveIcon;
        if (sprite == null) sprite = synergy.icon;

        if (badge != null)
        {
            badge.sprite  = sprite;
            badge.enabled = sprite != null; // no white box when unassigned
        }
        if (nameText != null) nameText.text = synergy.synergyName; // name unchanged in both states

        // Active: big count beside the badge + tier breakpoints. Inactive: no big count + "current/next".
        GameObject bigRoot = largeCountGroup != null ? largeCountGroup
                           : largeCountText != null ? largeCountText.gameObject : null;
        if (bigRoot != null) bigRoot.SetActive(active);
        if (active && largeCountText != null) largeCountText.text = entry.currentCount.ToString();
        if (countText != null)
            countText.text = active ? BuildBreakpoints(entry) : BuildCount(entry);

        if (canvasGroup != null) canvasGroup.alpha = active ? 1f : inactiveAlpha;
    }

    /// <summary>Hide this row (layout group reclaims the space).</summary>
    public void Hide() => gameObject.SetActive(false);

    // Hover tooltip //

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltip != null && hasEntry) tooltip.Show(currentEntry, (RectTransform)transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltip != null) tooltip.RequestHide(); // deferred so the cursor can reach the tooltip
    }

    // Tier breakpoints "2 › 4 › 6 › 8"; the active tier's number is highlighted, the rest dimmed. //
    private string BuildBreakpoints(SynergyEntry entry)
    {
        SynergyTier[] tiers = entry.synergy.tiers;
        if (tiers == null || tiers.Length == 0) return "";

        var sb = new StringBuilder();
        for (int i = 0; i < tiers.Length; i++)
        {
            if (i > 0) sb.Append($"<color={dimHex}> › </color>");
            if (i == entry.activeTierIndex)
                sb.Append(tiers[i].requiredCount);                                  // current tier: bright
            else
                sb.Append($"<color={dimHex}>{tiers[i].requiredCount}</color>");     // others: dim
        }
        return sb.ToString();
    }

    // "current / next breakpoint" — next is the first unmet tier, or the top tier when maxed. //
    private static string BuildCount(SynergyEntry entry)
    {
        SynergyTier[] tiers = entry.synergy.tiers;
        if (tiers == null || tiers.Length == 0) return entry.currentCount.ToString();

        int next = tiers[tiers.Length - 1].requiredCount;
        for (int i = 0; i < tiers.Length; i++)
            if (tiers[i].requiredCount > entry.currentCount) { next = tiers[i].requiredCount; break; }

        return $"{entry.currentCount}/{next}";
    }
}
