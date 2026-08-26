using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// One unit thumbnail cell in the synergy tooltip's bottom row. Bound to a UnitData;
/// shows the unit's portrait (or a placeholder when empty) and pops a UnitTooltip on hover.
/// </summary>
public class SynergyThumbnail : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image image;                // inner icon (portrait / placeholder)
    [SerializeField] private Image border;               // cost-colored frame
    [SerializeField] private ShopCostPalette palette;    // shared cost → color table (same as shop)

    private UnitData unit;
    private UnitTooltip tooltip;

    private Sprite placeholderSprite; // cached defaults for the "empty" look on reuse
    private Color placeholderColor;
    private bool cached;

    private void Awake()
    {
        if (image != null) { placeholderSprite = image.sprite; placeholderColor = image.color; cached = true; }
    }

    /// <summary>Bind this cell to a unit and the shared unit tooltip.</summary>
    public void Bind(UnitData u, UnitTooltip t)
    {
        unit = u;
        tooltip = t;

        // Cost-colored border (same palette as the shop).
        if (border != null && palette != null && u != null)
            border.color = palette.ColorFor(u.cost);

        if (image == null) return;

        if (u != null && u.thumbnail != null)     // art available
        {
            image.sprite = u.thumbnail;
            image.color  = Color.white;
        }
        else if (cached)                          // empty → placeholder look
        {
            image.sprite = placeholderSprite;
            image.color  = placeholderColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltip != null && unit != null) tooltip.Show(unit, (RectTransform)transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltip != null) tooltip.Hide();
    }
}
