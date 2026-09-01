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
    [SerializeField] private Image glow;                  // soft glow, lit when already owned
    [SerializeField] private Color glowColor = Color.white; // glow tint (alpha comes from the pulse)

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
        if (priceText != null) priceText.text = filled ? $"<sprite=0>{data.cost}"  : string.Empty;

        // Synergy Rows //
        int count = filled && data.synergies != null ? data.synergies.Length : 0;
        for (int i = 0; i < synergyRows.Length; i++)
        {
            if (synergyRows[i] == null) continue;
            if (i < count) synergyRows[i].Set(data.synergies[i]);
            else           synergyRows[i].Hide();
        }

        ApplyCostStyle(filled ? data.cost : 0, filled, filled && PlayerOwnsCopy(data));
    }

    /// <summary>Tint the cost frame and toggle/tint the glow (lit when a copy is already owned).</summary>
    private void ApplyCostStyle(int cost, bool filled, bool owned)
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
            glow.gameObject.SetActive(owned);
            if (owned) glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, glow.color.a); // Inspector tint, pulse alpha
        }
    }

    /// <summary>True if the player already owns this unit on the board or bench (matched by name, so star tiers count).</summary>
    private static bool PlayerOwnsCopy(UnitData data)
    {
        if (data == null) return false;
        string name = data.unitName;

        if (UnitManager.Instance != null)
            foreach (UnitController u in UnitManager.Instance.playerUnits)
                if (u != null && u.Stats != null && u.Stats.UnitData != null && u.Stats.UnitData.unitName == name)
                    return true;

        if (BenchManager.Instance != null)
            foreach (UnitController u in BenchManager.Instance.benchUnits)
                if (u != null && u.Stats != null && u.Stats.UnitData != null && u.Stats.UnitData.unitName == name)
                    return true;

        return false;
    }
}
