using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Binds one shop slot's visuals (portrait, name, price, synergy rows) from UnitData.
/// Synergy rows are a fixed pool; unused rows are hidden. Driven by ShopUI.
/// </summary>
public class ShopSlotUI : MonoBehaviour
{
    [Header("Elements")]
    [SerializeField] private Image unitImage;             // unit portrait
    [SerializeField] private TextMeshProUGUI nameText;    // unit name
    [SerializeField] private TextMeshProUGUI priceText;   // unit cost

    [Header("Synergy Rows")]
    [SerializeField] private SynergyRowUI[] synergyRows;  // fixed pool (one per possible synergy)

    [Header("Cost Styling")]
    [SerializeField] private ShopCostPalette palette;     // shared cost → color table
    [SerializeField] private Image costFrame;             // colored border, tinted by cost
    [SerializeField] private Image glow;                  // soft glow, cost-tinted, high-cost only

    /// <summary>Show the given unit, or clear the slot when data is null.</summary>
    public void SetUnit(UnitData data)
    {
        bool filled = data != null;

        if (unitImage != null)
        {
            unitImage.sprite  = filled ? data.portrait : null;
            unitImage.enabled = filled && data.portrait != null; // hide when unassigned
        }
        if (nameText  != null) nameText.text  = filled ? data.unitName    : string.Empty;
        if (priceText != null) priceText.text = filled ? $"{data.cost}g"  : string.Empty;

        // Synergy Rows //
        int count = filled && data.synergies != null ? data.synergies.Length : 0;
        for (int i = 0; i < synergyRows.Length; i++)
        {
            if (synergyRows[i] == null) continue;
            if (i < count) synergyRows[i].Set(data.synergies[i]);
            else           synergyRows[i].Hide();
        }

        ApplyCostStyle(filled ? data.cost : 0, filled);
    }

    /// <summary>Tint the cost frame and toggle/tint the high-cost glow.</summary>
    private void ApplyCostStyle(int cost, bool filled)
    {
        if (!filled || palette == null)
        {
            if (costFrame != null) costFrame.enabled = false;
            if (glow != null) glow.gameObject.SetActive(false);
            return;
        }

        Color c = palette.ColorFor(cost);
        if (costFrame != null) { costFrame.enabled = true; costFrame.color = c; }

        if (glow != null)
        {
            bool shouldGlow = palette.ShouldGlow(cost);
            glow.gameObject.SetActive(shouldGlow);
            if (shouldGlow) glow.color = new Color(c.r, c.g, c.b, glow.color.a); // keep pulse-driven alpha
        }
    }
}
