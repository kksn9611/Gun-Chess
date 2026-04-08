using UnityEngine;
using System.Collections;

/// <summary>
/// 모든 스킬의 기반 클래스.
/// ScriptableObject로 만들어 UnitData에 슬롯으로 할당한다.
/// 각 스킬은 이 클래스를 상속하여 Execute()를 구현한다.
/// </summary>
public abstract class BaseSkill : ScriptableObject
{
    [Header("스킬 기본 정보")]
    [Tooltip("스킬 이름")]
    public string skillName = "기본 스킬";

    [Tooltip("스킬 설명")]
    [TextArea(2, 4)]
    public string description = "";

    [Tooltip("스킬 시전 시간")]
    public float castTime = 1f;

    /// <summary>
    /// 스킬 발동
    /// </summary>
    public abstract IEnumerator Execute(UnitController caster);
}
