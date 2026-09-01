using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bottom-center shop bar: 5 unit slots, reroll, and lock.
/// Subscribes to ShopManager events to refresh.
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShopManager shop;

    [Header("Slots")]
    [SerializeField] private Button[] slotButtons;
    [SerializeField] private ShopSlotUI[] slotUIs;

    [Header("Controls")]
    [SerializeField] private Button rerollButton;
    [SerializeField] private TextMeshProUGUI rerollLabel;
    [SerializeField] private Button lockButton;
    [SerializeField] private TextMeshProUGUI lockLabel;


    private void Awake()
    {
        // Wire slot buttons (capture index per slot)
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;
            if (slotButtons[i] != null)
                slotButtons[i].onClick.AddListener(() => shop.Purchase(index));
        }

        if (rerollButton != null) rerollButton.onClick.AddListener(OnReroll);
        if (lockButton != null) lockButton.onClick.AddListener(OnLock);
    }

    private void OnEnable()
    {
        if (shop == null) return;
        shop.OnShopChanged       += RefreshSlots;
        shop.OnLockChanged       += UpdateLock;
        shop.OnFreeRerollChanged += UpdateReroll;

        // Pull current state in case events fired before this enabled
        RefreshSlots();
        UpdateLock(shop.IsLocked);
        UpdateReroll(shop.FreeRerolls);
    }

    private void OnDisable()
    {
        if (shop == null) return;
        shop.OnShopChanged       -= RefreshSlots;
        shop.OnLockChanged       -= UpdateLock;
        shop.OnFreeRerollChanged -= UpdateReroll;
    }


    // Button Handlers //

    private void OnReroll() { if (shop != null) shop.Reroll(); }
    private void OnLock()   { if (shop != null) shop.ToggleLock(); }


    // UI Update //

    /// <summary>Refresh every slot label and interactability from the shop.</summary>
    private void RefreshSlots()
    {
        if (shop == null) return;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            UnitData unit = shop.GetSlotUnit(i);
            bool filled = unit != null;

            if (slotButtons[i] != null) slotButtons[i].interactable = filled;
            if (i < slotUIs.Length && slotUIs[i] != null) slotUIs[i].SetUnit(unit);
        }
    }

    private void UpdateLock(bool locked)
    {
        if (lockLabel != null) lockLabel.text = locked ? "Locked" : "Lock";
    }

    private void UpdateReroll(int freeRerolls)
    {
        if (rerollLabel == null || shop == null) return;
        rerollLabel.text = freeRerolls > 0
            ? $"리롤 <sprite=0>0 ({freeRerolls}회)"
            : $"리롤 <sprite=0>{shop.RerollCost}";
    }
}
