using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// TopBar square indicator that reveals the current stage's enemy modifiers on hover, and tints its icon
/// by the total modifier percentage (higher total = more severe color).
/// Modular: the icon and the tooltip are plain serialized refs, so the visuals can be swapped or
/// restyled freely (or the whole thing prefabbed) without touching this logic.
/// </summary>
public class EnemyModifierIndicator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private GameObject tooltip;           // panel shown while hovering
    [SerializeField] private TextMeshProUGUI tooltipText;  // filled from the stage's enemyBuffs
    [SerializeField] private string emptyText = "적 버프 없음";

    [Header("Dynamic Color")]
    [SerializeField] private Image icon;                   // recolored by total modifier % (defaults to this)
    [SerializeField] private Gradient severityGradient;    // evaluated 0 .. maxTotalPercent
    [Tooltip("Total % at which the gradient reaches its far end")]
    [SerializeField] private float maxTotalPercent = 200f;

    private readonly StringBuilder sb = new StringBuilder();
    private int lastRound = int.MinValue;

    private void Awake()
    {
        if (tooltip != null) tooltip.SetActive(false);
        if (icon == null) icon = GetComponent<Image>();
        RefreshColor();
    }

    // Re-tint when the stage changes (cheap round-number poll).
    private void Update()
    {
        int round = roundManager != null ? roundManager.CurrentRound : -1;
        if (round == lastRound) return;
        lastRound = round;
        RefreshColor();
    }

    /// <summary>Tint the icon by the current stage's total modifier percentage.</summary>
    private void RefreshColor()
    {
        if (icon == null || severityGradient == null) return;
        float t = maxTotalPercent > 0f ? Mathf.Clamp01(TotalModifierPercent() / maxTotalPercent) : 0f;
        icon.color = severityGradient.Evaluate(t);
    }

    /// <summary>Sum of every enemy modifier's percent for the current stage (0 if none).</summary>
    private float TotalModifierPercent()
    {
        StageData stage = roundManager != null ? roundManager.CurrentStage : null;
        StatBoostEntry[] buffs = stage != null ? stage.enemyBuffs : null;
        if (buffs == null) return 0f;
        float sum = 0f;
        foreach (StatBoostEntry b in buffs) sum += b.percentBoost;
        return sum;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipText != null) tooltipText.text = BuildText();
        if (tooltip != null) tooltip.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltip != null) tooltip.SetActive(false);
    }

    /// <summary>One line per enemy buff ("Att +30%"), or the empty message.</summary>
    private string BuildText()
    {
        sb.Clear();
        StageData stage = roundManager != null ? roundManager.CurrentStage : null;
        StatBoostEntry[] buffs = stage != null ? stage.enemyBuffs : null;
        if (buffs == null || buffs.Length == 0) return emptyText;
        sb.Append("적 버프");
        foreach (StatBoostEntry b in buffs)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(b.statType.ToKorean()).Append(" +").Append(b.percentBoost).Append('%');
        }
        return sb.ToString();
    }
}
