using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Shared hover tooltip for synergy badges. One instance, reused by every badge.
/// Inactive synergy → shows SynergyData.description. Active synergy → per-tier lines,
/// with the active tier bolded and colored via activeTierColor / lockedTierColor.
/// </summary>
public class SynergyTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private RectTransform panel;       // root shown/hidden + positioned (defaults to this)
    [SerializeField] private TextMeshProUGUI nameText;  // synergy name header
    [SerializeField] private TextMeshProUGUI bodyText;  // description / tier lines
    [Tooltip("Bottom container for square unit thumbnails")]
    [SerializeField] private RectTransform thumbnailContainer;
    [Tooltip("Catalog scanned for units belonging to a synergy")]
    [SerializeField] private UnitPoolDatabase database;
    [Tooltip("Thumbnail cell prefab (SynergyThumbnail)")]
    [SerializeField] private SynergyThumbnail thumbnailCellPrefab;
    [Tooltip("Shared unit tooltip shown when hovering a thumbnail")]
    [SerializeField] private UnitTooltip unitTooltip;

    private readonly List<SynergyThumbnail> thumbPool = new List<SynergyThumbnail>();
    private readonly List<UnitData> matchBuffer = new List<UnitData>();

    [Header("Tier State Colors")]
    [Tooltip("Color of the currently-active tier line (also bold)")]
    [SerializeField] private Color activeTierColor = Color.white;
    [Tooltip("Color of locked (unreached) tier lines")]
    [SerializeField] private Color lockedTierColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Header("Placement")]
    [Tooltip("Offset from the hovered badge's right edge")]
    [SerializeField] private Vector2 offset = new Vector2(12f, 0f);

    [Header("Hover")]
    [Tooltip("Grace period before hiding, so the cursor can travel from the badge onto the tooltip")]
    [SerializeField] private float hideDelay = 0.2f;

    private Coroutine hideRoutine;

    private void Awake()
    {
        if (panel == null) panel = (RectTransform)transform;
        Hide();
    }

    // Show / Hide //

    public void Show(SynergyEntry entry, RectTransform anchor)
    {
        CancelHide();

        SynergyData synergy = entry.synergy;
        if (synergy == null) { Hide(); return; }

        if (nameText != null) nameText.text = synergy.synergyName;
        if (bodyText != null) bodyText.text = BuildBody(entry);

        PopulateThumbnails(synergy);

        panel.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel); // size before positioning
        PositionBeside(anchor);
    }

    /// <summary>Bind one thumbnail cell per base unit that has this synergy; hide the rest.</summary>
    private void PopulateThumbnails(SynergyData synergy)
    {
        if (thumbnailContainer == null || thumbnailCellPrefab == null || database == null) return;

        // Collect units in this synergy, then sort by cost (ascending; name as tiebreak).
        matchBuffer.Clear();
        if (database.baseUnits != null)
            foreach (UnitData u in database.baseUnits)
            {
                if (u == null || u.synergies == null) continue;
                if (System.Array.IndexOf(u.synergies, synergy) < 0) continue;
                matchBuffer.Add(u);
            }
        matchBuffer.Sort((a, b) =>
        {
            int c = a.cost.CompareTo(b.cost);
            return c != 0 ? c : string.CompareOrdinal(a.unitName, b.unitName);
        });

        int i = 0;
        for (; i < matchBuffer.Count; i++)
        {
            SynergyThumbnail cell = GetThumb(i);
            cell.Bind(matchBuffer[i], unitTooltip);
            cell.gameObject.SetActive(true);
        }
        for (; i < thumbPool.Count; i++) thumbPool[i].gameObject.SetActive(false);
    }

    private SynergyThumbnail GetThumb(int index)
    {
        while (thumbPool.Count <= index)
            thumbPool.Add(Instantiate(thumbnailCellPrefab, thumbnailContainer));
        SynergyThumbnail t = thumbPool[index];
        t.transform.SetSiblingIndex(index);
        return t;
    }

    /// <summary>Hide after a short grace period unless the cursor reaches the tooltip first.</summary>
    public void RequestHide()
    {
        CancelHide();
        if (isActiveAndEnabled && panel != null && panel.gameObject.activeInHierarchy)
            hideRoutine = StartCoroutine(HideAfter(hideDelay));
        else
            Hide();
    }

    public void Hide()
    {
        CancelHide();
        HidePanelNow();
    }

    private void HidePanelNow()
    {
        if (unitTooltip != null) unitTooltip.Hide(); // dismiss the nested unit tooltip too
        if (panel != null) panel.gameObject.SetActive(false);
    }

    private IEnumerator HideAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        hideRoutine = null;
        HidePanelNow();
    }

    private void CancelHide()
    {
        if (hideRoutine != null) { StopCoroutine(hideRoutine); hideRoutine = null; }
    }

    // Keep the tooltip open while the cursor is over it. //
    public void OnPointerEnter(PointerEventData eventData) => CancelHide();
    public void OnPointerExit(PointerEventData eventData)  => RequestHide();

    // Body //

    private string BuildBody(SynergyEntry entry)
    {
        SynergyData synergy = entry.synergy;

        var sb = new StringBuilder();

        // General description at the very top (both active and inactive).
        if (!string.IsNullOrEmpty(synergy.description))
            sb.Append(synergy.description).Append("\n\n");

        // Tier lines for both states. Inactive → activeTierIndex is -1, so every tier renders as locked.
        SynergyTier[] tiers = synergy.tiers;
        if (tiers != null)
        {
            for (int i = 0; i < tiers.Length; i++)
            {
                if (i > 0) sb.Append('\n');
                string line = $"{tiers[i].requiredCount}  {tiers[i].description}";

                if (i == entry.activeTierIndex)
                    sb.Append($"<b>{Wrap(line, activeTierColor)}</b>"); // active: bold + color
                else if (i > entry.activeTierIndex)
                    sb.Append(Wrap(line, lockedTierColor));             // locked / all-inactive: color
                else
                    sb.Append(line);                                    // met (below active): as authored
            }
        }
        return sb.ToString();
    }

    private static string Wrap(string s, Color c)
        => $"<color=#{ColorUtility.ToHtmlStringRGBA(c)}>{s}</color>";

    // Placement — right of the badge, clamped to the screen. //

    private static readonly Vector3[] corners = new Vector3[4];

    private void PositionBeside(RectTransform anchor)
    {
        panel.pivot = new Vector2(0f, 0.5f); // grow rightward from the anchor

        anchor.GetWorldCorners(corners);      // 0=BL 1=TL 2=TR 3=BR
        Vector3 rightMid = (corners[2] + corners[3]) * 0.5f;
        panel.position = rightMid + (Vector3)offset;

        // Clamp within the screen (overlay canvas → world corners are screen pixels)
        panel.GetWorldCorners(corners);
        float minX = corners[0].x, maxX = corners[2].x, minY = corners[0].y, maxY = corners[1].y;
        float dx = 0f, dy = 0f;
        if (maxX > Screen.width)  dx = Screen.width - maxX;
        if (minX + dx < 0f)       dx = -minX;
        if (maxY > Screen.height) dy = Screen.height - maxY;
        if (minY + dy < 0f)       dy = -minY;
        panel.position += new Vector3(dx, dy, 0f);
    }
}
