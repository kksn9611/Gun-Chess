using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spends gold to gain EXP on click. Costs/amounts set in Inspector.
/// </summary>
public class BuyExpButton : MonoBehaviour
{
    [SerializeField] private Button buyButton;
    [SerializeField] private int goldCost = 4;   // Gold spent per click
    [SerializeField] private int expGain = 4;     // EXP gained per click

    private void Awake()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(BuyExp);
    }

    private void BuyExp()
    {
        if (PlayerManager.Instance == null) return;
        if (PlayerManager.Instance.IsMaxLevel)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayUi(SoundId.UiError);
            return;
        }
        if (PlayerManager.Instance.TrySpendGold(goldCost))
        {
            PlayerManager.Instance.AddExp(expGain);
            if (SoundManager.Instance != null) SoundManager.Instance.PlayUi(SoundId.UiPurchase);
        }
        else
        {
            Debug.Log("[BuyExpButton] Not enough gold");
            if (SoundManager.Instance != null) SoundManager.Instance.PlayUi(SoundId.UiError);
        }
    }

    private void OnDestroy()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(BuyExp);
    }
}
