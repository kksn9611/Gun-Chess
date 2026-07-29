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
    }
}
