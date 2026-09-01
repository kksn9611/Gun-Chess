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
    [SerializeField] private Material grayscaleMaterial; // desaturates the icon when the unit isn't on the board
    [SerializeField] private Color offBoardTint = new Color(0.5f, 0.5f, 0.5f, 1f); // dims the grayscale icon

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
    /// <param name="onBoard">True if a copy of this unit is currently on the field; drives color vs grayscale.</param>
    public void Bind(UnitData u, UnitTooltip t, bool onBoard)
    {
        unit = u;
        tooltip = t;

        // Cost-colored border (same palette as the shop).
        if (border != null && palette != null && u != null)
            border.color = palette.ColorFor(u.cost);

        if (image == null) return;

        // Color when a copy is on the board, grayscale otherwise.
        image.material = onBoard ? null : grayscaleMaterial;

        if (u != null && u.thumbnail != null)     // art available
        {
            image.sprite = u.thumbnail;
            image.color  = onBoard ? Color.white : offBoardTint; // dim when off-board
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
