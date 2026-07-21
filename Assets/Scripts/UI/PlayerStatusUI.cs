using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays player gold, level, and EXP (current / required).
/// Subscribes to PlayerManager static events for auto-refresh.
/// </summary>
public class PlayerStatusUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private Image expBarFill; // filled bar, 0..1 toward next level

    private void OnEnable()
    {
        PlayerManager.OnGoldChanged  += UpdateGold;
        PlayerManager.OnLevelChanged += UpdateLevel;
        PlayerManager.OnExpChanged   += UpdateExp;

        // Pull current state in case events already fired before this enabled
        if (PlayerManager.Instance != null)
        {
            UpdateGold(PlayerManager.Instance.Gold);
            UpdateLevel(PlayerManager.Instance.CurrentLevel);
            UpdateExp(PlayerManager.Instance.CurrentExp, PlayerManager.Instance.ExpToNextLevel);
        }
    }

    private void OnDisable()
    {
        PlayerManager.OnGoldChanged  -= UpdateGold;
        PlayerManager.OnLevelChanged -= UpdateLevel;
        PlayerManager.OnExpChanged   -= UpdateExp;
    }


    // Update Handlers //

    private void UpdateGold(int gold)
    {
        if (goldText != null) goldText.text = $"{gold}";
    }

    private void UpdateLevel(int level)
    {
        if (levelText != null) levelText.text = $"{level}레벨";
    }

    private void UpdateExp(int currentExp, int requiredExp)
    {
        if (expText != null)
            expText.text = requiredExp > 0
                ? $"{currentExp} / {requiredExp}"
                : "MAX";

        if (expBarFill != null)
            expBarFill.fillAmount = requiredExp > 0 ? (float)currentExp / requiredExp : 1f;
    }
}
