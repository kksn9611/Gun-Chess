using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One synergy row in a shop slot: icon + name. Bound by ShopSlotUI.
/// </summary>
public class SynergyRowUI : MonoBehaviour
{
    [SerializeField] private Image icon;              // synergy icon
    [SerializeField] private TextMeshProUGUI label;   // synergy name

    /// <summary>Show this synergy, or hide the row when null.</summary>
    public void Set(SynergyData synergy)
    {
        gameObject.SetActive(synergy != null);
        if (synergy == null) return;

        if (label != null) label.text = synergy.synergyName;
        if (icon != null)
        {
            icon.sprite  = synergy.icon;
            icon.enabled = synergy.icon != null; // no white box when unassigned
        }
    }

    /// <summary>Collapse the row (layout group reclaims the space).</summary>
    public void Hide() => gameObject.SetActive(false);
}
