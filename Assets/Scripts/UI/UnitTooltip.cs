using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Small tooltip shown when hovering a unit thumbnail: the unit's name + its synergy trait names.
/// One shared instance, positioned above the hovered thumbnail.
/// </summary>
public class UnitTooltip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform panel;      // shown/hidden + positioned (defaults to this)
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI bodyText;  // synergy trait names

    [Header("Placement")]
    [Tooltip("Offset from the thumbnail's top edge")]
    [SerializeField] private Vector2 offset = new Vector2(0f, 10f);

    private void Awake()
    {
        if (panel == null) panel = (RectTransform)transform;
        Hide();
    }

    public void Show(UnitData unit, RectTransform anchor)
    {
        if (unit == null) { Hide(); return; }

        if (nameText != null) nameText.text = unit.unitName;
        if (bodyText != null) bodyText.text = BuildSynergies(unit);

        panel.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        PositionAbove(anchor);
    }

    public void Hide()
    {
        if (panel != null) panel.gameObject.SetActive(false);
    }

    private static string BuildSynergies(UnitData unit)
    {
        if (unit.synergies == null || unit.synergies.Length == 0) return "";

        var sb = new StringBuilder();
        foreach (SynergyData s in unit.synergies)
        {
            if (s == null) continue;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(s.synergyName);
        }
        return sb.ToString();
    }

    private static readonly Vector3[] corners = new Vector3[4];

    private void PositionAbove(RectTransform anchor)
    {
        panel.pivot = new Vector2(0.5f, 0f); // grow upward from the thumbnail

        anchor.GetWorldCorners(corners);      // 0=BL 1=TL 2=TR 3=BR
        Vector3 topMid = (corners[1] + corners[2]) * 0.5f;
        panel.position = topMid + (Vector3)offset;

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
