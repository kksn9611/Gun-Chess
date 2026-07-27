using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Displays predicted gold income for the next turn (base + interest + synergy).
/// Sits next to the current gold display. Hovering shows a breakdown tooltip at the
/// top-right of the mouse. Refreshes when gold or synergies change.
/// </summary>
public class NextTurnGoldUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI totalText;      // predicted next-turn total, e.g. "+8"
    [SerializeField] private SynergyState synergyState;      // fires OnSynergyChanged when synergy income changes
    [SerializeField] private SynergyManager synergyManager;  // computes synergy round income

    [Header("Tooltip")]
    [SerializeField] private RectTransform tooltipRoot;      // breakdown panel, shown only while hovering
    [SerializeField] private TextMeshProUGUI breakdownText;  // per-source breakdown inside the tooltip
    [SerializeField] private Vector2 tooltipOffset = new Vector2(16f, 16f); // top-right offset from the cursor

    private bool hovering; // tooltip visible, following the cursor

    private void OnEnable()
    {
        PlayerManager.OnGoldChanged += OnGoldChanged; // interest depends on held gold
        if (synergyState != null) synergyState.OnSynergyChanged += Recalculate;

        Recalculate(); // pull current state in case events already fired
        HideTooltip();
    }

    private void OnDisable()
    {
        PlayerManager.OnGoldChanged -= OnGoldChanged;
        if (synergyState != null) synergyState.OnSynergyChanged -= Recalculate;
    }

    private void Update()
    {
        if (hovering) PositionTooltip(); // follow the cursor to the top-right
    }


    // Update //

    private void OnGoldChanged(int _) => Recalculate();

    /// <summary>Recompute predicted income; refresh the label and (if visible) the breakdown.</summary>
    private void Recalculate()
    {
        int baseGold = 0, interest = 0, synergy = 0;
        if (PlayerManager.Instance != null)
        {
            baseGold = PlayerManager.Instance.BaseTurnGold;
            interest = PlayerManager.Instance.CalculateInterest();
        }
        if (synergyManager != null) synergy = synergyManager.CalculateRoundIncome();

        if (totalText != null) totalText.text = $"+{baseGold + interest + synergy}";
        if (hovering) UpdateBreakdown(baseGold, interest, synergy);
    }


    // Tooltip //

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        Recalculate(); // fills the breakdown before showing
        if (tooltipRoot != null) tooltipRoot.gameObject.SetActive(true);
        PositionTooltip();
    }

    public void OnPointerExit(PointerEventData eventData) => HideTooltip();

    private void HideTooltip()
    {
        hovering = false;
        if (tooltipRoot != null) tooltipRoot.gameObject.SetActive(false);
    }

    /// <summary>Place the tooltip to the top-right of the cursor.</summary>
    private void PositionTooltip()
    {
        if (tooltipRoot == null || Pointer.current == null) return;
        Vector2 mouse = Pointer.current.position.ReadValue();
        tooltipRoot.position = new Vector3(mouse.x + tooltipOffset.x, mouse.y + tooltipOffset.y, 0f);
    }

    /// <summary>Write the per-source breakdown into the tooltip text.</summary>
    private void UpdateBreakdown(int baseGold, int interest, int synergy)
    {
        if (breakdownText == null) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>[골드 획득량]</b>");
        sb.AppendLine($"기본    +{baseGold}");
        if (interest != 0) sb.AppendLine($"이자    +{interest}");
        if (synergy != 0) sb.AppendLine($"시너지 +{synergy}");
        sb.Append($"<b>합계   +{baseGold + interest + synergy}</b>");
        breakdownText.text = sb.ToString();
    }
}
