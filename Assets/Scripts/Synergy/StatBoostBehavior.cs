using UnityEngine;

/// <summary>
/// 여러 스탯을 % 단위로 보정하는 시너지 효과 에셋.
/// Inspector에서 boosts 배열에 스탯 종류와 보정 비율을 여러 개 설정할 수 있다.
/// </summary>
[CreateAssetMenu(fileName = "StatBoost", menuName = "Scriptable Objects/Synergy/StatBoostBehavior")]
public class StatBoostBehavior : SynergyBehavior
{
    [Tooltip("보정할 스탯 목록")]
    public StatBoostEntry[] boosts;

    public override void Apply(UnitController unit)
    {
        if (boosts == null) return;
        foreach (var entry in boosts)
            unit.ApplyStatModifier(entry.statType, entry.percentBoost);
    }

    public override void Remove(UnitController unit)
    {
        if (boosts == null) return;
        foreach (var entry in boosts)
            unit.ApplyStatModifier(entry.statType, -entry.percentBoost);
    }
}

/// <summary>
/// 스탯 보정 항목 하나. 스탯 종류와 보정 비율을 정의한다.
/// </summary>
[System.Serializable]
public struct StatBoostEntry
{
    [Tooltip("보정할 스탯 종류")]
    public StatType statType;

    [Tooltip("보정 비율 (%). 예: 20 = 20% 증가")]
    public float percentBoost;
}
