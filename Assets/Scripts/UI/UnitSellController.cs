using UnityEngine;

/// <summary>
/// Sell drop-zone on the BottomBar. While a unit is dragged over the bar it swaps the bar UI
/// (PlayerStatus/Shop) for a sell notice; dropping there sells the unit for its sellPrice.
/// </summary>
public class UnitSellController : MonoBehaviour
{
    [Header("Sell Zone")]
    [SerializeField] private RectTransform sellZone;   // defaults to this RectTransform
    [SerializeField] private Camera canvasCamera;      // null for Screen Space - Overlay

    [Header("BottomBar UI (hidden during sell)")]
    [SerializeField] private GameObject playerStatusUI;
    [SerializeField] private GameObject shopUI;
    [Header("Sell UI")]
    [SerializeField] private GameObject sellNotice;    // "Sell Unit" text, centered

    private bool sellMode;

    private void Awake()
    {
        if (sellZone == null) sellZone = GetComponent<RectTransform>();
        SetSellMode(false);
    }

    // Zone //

    /// <summary>True when the screen point is over the sell zone.</summary>
    public bool Contains(Vector2 screenPos)
        => sellZone != null && RectTransformUtility.RectangleContainsScreenPoint(sellZone, screenPos, canvasCamera);

    /// <summary>Swap the bar UI for the sell notice (or restore it).</summary>
    public void SetSellMode(bool on)
    {
        if (sellMode == on) return;
        sellMode = on;
        if (playerStatusUI != null) playerStatusUI.SetActive(!on);
        if (shopUI != null)         shopUI.SetActive(!on);
        if (sellNotice != null)     sellNotice.SetActive(on);
    }

    // Sell //

    /// <summary>Sell a unit: refund gold, return copies to the pool, despawn. False if not sellable.</summary>
    public bool Sell(UnitController unit)
    {
        if (unit == null || unit.CurrentTeam != Team.Player) return false;
        UnitData data = unit.Stats != null ? unit.Stats.UnitData : null;

        // Release the tile it still occupies (pickup does not clear occupancy)
        BaseTile tile = unit.CurrentTile;
        if (tile != null) tile.IsOccupied = false;

        // Drop it from the owning manager
        if (unit.IsOnBench) BenchManager.Instance.RemoveUnit(unit);
        else                UnitManager.Instance.RemoveUnit(unit, unit.CurrentTeam);

        // Return copies to the shop pool and refund
        if (data != null)
        {
            if (UnitPool.Instance != null) UnitPool.Instance.Return(data);
            if (PlayerManager.Instance != null) PlayerManager.Instance.AddGold(data.sellPrice);
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlayUi(SoundId.UiSell);

        Destroy(unit.gameObject);
        return true;
    }
}
