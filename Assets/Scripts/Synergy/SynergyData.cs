using UnityEngine;

/// <summary>
/// 시너지 이름, 설명, 구간별 활성 조건과 효과를 담는 ScriptableObject.
/// </summary>
[CreateAssetMenu(fileName = "SynergyData", menuName = "Scriptable Objects/SynergyData")]
public class SynergyData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("시너지 이름")]
    public string synergyName = "New Synergy";

    [Header("시너지 설명")]
    [TextArea(2, 4)]
    public string description = "";

    [Header("구간 설정")]
    [Tooltip("활성 구간 배열. requiredCount 오름차순으로 설정한다.")]
    public SynergyTier[] tiers;

    /// <summary>
    /// 현재 활성화된 유닛 수
    /// </summary>
    public int GetActiveTierIndex(int unitCount)
    {
        int activeIndex = -1;
        for (int i = 0; i < tiers.Length; i++)
        {
            if (unitCount >= tiers[i].requiredCount)
                activeIndex = i;
            else
                break; // requiredCount 오름차순이므로 이후 구간은 모두 미달
        }
        return activeIndex;
    }
}

/// <summary>
/// 시너지 구간 설정
/// </summary>
[System.Serializable]
public struct SynergyTier
{
    [Tooltip("필요한 시너지 유닛 수")]
    public int requiredCount;

    [Tooltip("이 구간에서 적용할 효과 에셋 리스트 (StatBoost, 특수 효과 등)")]
    public SynergyBehavior[] behaviors;
}
