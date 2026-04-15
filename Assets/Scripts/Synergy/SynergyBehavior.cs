using UnityEngine;

/// <summary>
/// 시너지 효과의 기반 추상 ScriptableObject.
/// SynergyTier.behaviors 배열에 에셋을 드래그&드롭하여 조립한다.
/// </summary>
public abstract class SynergyBehavior : ScriptableObject
{
    /// <summary>
    /// 시너지 활성화 효과를 적용
    /// </summary>
    public abstract void Apply(UnitController unit);

    /// <summary>
    /// 시너지 비활성화 Apply()에서 적용한 보정을 정확히 원복
    /// </summary>
    public abstract void Remove(UnitController unit);
}
