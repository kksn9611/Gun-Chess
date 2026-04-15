using UnityEngine;
using TMPro;

/// <summary>
/// 화면 좌측에 현재 활성 시너지 목록과 단계를 텍스트로 표시한다.
/// SynergyState SO의 OnSynergyChanged 이벤트를 구독하여 자동 갱신한다.
/// </summary>
public class SynergyUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("시너지 상태 공유 데이터")]
    [SerializeField] private SynergyState synergyState;

    [Tooltip("시너지 정보를 표시할 TMP 텍스트")]
    [SerializeField] private TextMeshProUGUI synergyText;

    private void OnEnable()
    {
        if (synergyState != null)
            synergyState.OnSynergyChanged += UpdateUI;
    }

    private void OnDisable()
    {
        if (synergyState != null)
            synergyState.OnSynergyChanged -= UpdateUI;
    }

    /// <summary>
    /// 시너지 상태가 변경될 때 호출된다.
    /// 활성 시너지를 텍스트로 표시한다.
    /// </summary>
    private void UpdateUI()
    {
        if (synergyText == null || synergyState == null) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>[ 시너지 ]</b>");
        sb.AppendLine();

        foreach (var entry in synergyState.Entries)
        {
            if (entry.synergy == null) continue;

            // 활성 여부에 따라 색상 변경
            string color = entry.activeTierIndex >= 0 ? "#FFD700" : "#888888";
            string tierLabel = entry.activeTierIndex >= 0
                ? $"Tier {entry.activeTierIndex + 1}"
                : "-";

            // 구간 요구 수 표시 (예: 2/4/6)
            string thresholds = "";
            if (entry.synergy.tiers != null && entry.synergy.tiers.Length > 0)
            {
                var parts = new string[entry.synergy.tiers.Length];
                for (int i = 0; i < entry.synergy.tiers.Length; i++)
                {
                    // 현재 활성 구간은 강조
                    if (i == entry.activeTierIndex)
                        parts[i] = $"<b>{entry.synergy.tiers[i].requiredCount}</b>";
                    else
                        parts[i] = $"{entry.synergy.tiers[i].requiredCount}";
                }
                thresholds = $" ({string.Join("/", parts)})";
            }

            sb.AppendLine($"<color={color}>{entry.synergy.synergyName}</color>");
            sb.AppendLine($"  {entry.currentCount}기 | {tierLabel}{thresholds}");
        }

        // 시너지가 없을 때
        if (synergyState.Entries.Count == 0)
            sb.AppendLine("<color=#888888>배치된 시너지 없음</color>");

        synergyText.text = sb.ToString();
    }
}
