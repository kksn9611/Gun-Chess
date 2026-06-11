using UnityEngine;
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
        if (goldText != null) goldText.text = $"Gold: {gold}";
    }

    private void UpdateLevel(int level)
    {
        if (levelText != null) levelText.text = $"Level: {level}";
    }

    private void UpdateExp(int currentExp, int requiredExp)
    {
        if (expText == null) return;
        expText.text = requiredExp > 0
            ? $"EXP: {currentExp} / {requiredExp}"
            : "EXP: MAX";
    }
}
